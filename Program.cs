using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using question_answer.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Load .env from backend root for deployed environments where env vars
// are provided via file instead of host-level configuration.
var envFilePath = Path.Combine(builder.Environment.ContentRootPath, "..", ".env");
if (File.Exists(envFilePath))
{
    DotNetEnv.Env.Load(envFilePath);
}

// Add services to the container.
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (builder.Environment.IsProduction() && string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing database connection string. Set ConnectionStrings__DefaultConnection in .env or environment variables.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Question Answer API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

var jwtKey = builder.Configuration["Jwt:Key"] ?? Environment.GetEnvironmentVariable("Jwt__Key") ?? "super_secret_fallback_key_for_dev_mode_only_32_bytes";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? Environment.GetEnvironmentVariable("Jwt__Issuer") ?? "QuestionAnswerApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? Environment.GetEnvironmentVariable("Jwt__Audience") ?? "QuestionAnswerApp";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var dbContext = context.HttpContext.RequestServices.GetRequiredService<question_answer.Infrastructure.Data.AppDbContext>();
                var userIdStr = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdStr, out var userId))
                {
                    var user = await dbContext.Users.FindAsync(userId);
                    if (user == null || user.Status == question_answer.Domain.Enums.UserStatus.Blocked || user.Status == question_answer.Domain.Enums.UserStatus.Denied)
                    {
                        context.Fail("User account is disabled.");
                    }
                }
            }
        };
    });

builder.Services.AddAuthorization();

var frontendBaseUrl = builder.Configuration["Frontend:BaseUrl"] ?? Environment.GetEnvironmentVariable("Frontend__BaseUrl") ?? "http://localhost:3000";

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCorsPolicy", policy =>
    {
        policy.WithOrigins(frontendBaseUrl)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddScoped<question_answer.Application.Services.IAuthService, question_answer.Application.Services.AuthService>();
builder.Services.AddScoped<question_answer.Application.Services.IEmailService, question_answer.Application.Services.EmailService>();
builder.Services.AddScoped<question_answer.Application.Services.IDocxParserService, question_answer.Application.Services.DocxParserService>();
builder.Services.AddScoped<question_answer.Application.Services.IQuestionService, question_answer.Application.Services.QuestionService>();
builder.Services.AddScoped<question_answer.Application.Services.IExamService, question_answer.Application.Services.ExamService>();
builder.Services.AddScoped<question_answer.Application.Services.IExamTakingService, question_answer.Application.Services.ExamTakingService>();
builder.Services.AddScoped<question_answer.Application.Services.IAnalyticsService, question_answer.Application.Services.AnalyticsService>();
builder.Services.AddTransient<question_answer.Application.Services.DataSeeder>();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("GlobalLimit", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
});

builder.Services.AddControllers();

var app = builder.Build();

var autoMigrateSetting = Environment.GetEnvironmentVariable("DB__AUTO_MIGRATE")
    ?? builder.Configuration["Db:AutoMigrate"];
var autoMigrateEnabled = string.Equals(autoMigrateSetting, "true", StringComparison.OrdinalIgnoreCase)
    || (string.IsNullOrWhiteSpace(autoMigrateSetting) && builder.Environment.IsDevelopment());

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    
    if (autoMigrateEnabled)
    {
        try
        {
            // Apply migrations automatically when enabled.
            await context.Database.MigrateAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == "42501")
        {
            throw new InvalidOperationException(
                "Database user does not have permission to run migrations (SQLSTATE 42501). " +
                "Grant CREATE/USAGE on schema or set DB__AUTO_MIGRATE=false and run migrations separately.",
                ex);
        }
    }

    var seeder = services.GetRequiredService<question_answer.Application.Services.DataSeeder>();
    await seeder.SeedSuperAdminAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("FrontendCorsPolicy");

app.UseRateLimiter();

app.UseMiddleware<question_answer.Application.Services.AuditLogMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
