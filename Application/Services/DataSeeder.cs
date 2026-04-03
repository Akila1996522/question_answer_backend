using Microsoft.EntityFrameworkCore;
using question_answer.Domain.Entities;
using question_answer.Domain.Enums;
using question_answer.Infrastructure.Data;

namespace question_answer.Application.Services;

public class DataSeeder
{
    private readonly AppDbContext _context;

    public DataSeeder(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedSuperAdminAsync()
    {
        if (await _context.Users.AnyAsync(u => u.Email == "admin@example.com"))
        {
            return;
        }

        var superAdminId = Guid.NewGuid();
        var superAdmin = new User
        {
            Id = superAdminId,
            FirstName = "Super",
            LastName = "Admin",
            Email = "admin@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Status = UserStatus.Active
        };

        _context.Users.Add(superAdmin);

        var superAdminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        _context.UserRoles.Add(new UserRole
        {
            UserId = superAdminId,
            RoleId = superAdminRoleId
        });

        await _context.SaveChangesAsync();
    }
}
