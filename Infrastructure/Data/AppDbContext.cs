using Microsoft.EntityFrameworkCore;
using question_answer.Domain.Entities;
using question_answer.Domain.Enums;

namespace question_answer.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public virtual DbSet<User> Users => Set<User>();
    public virtual DbSet<Role> Roles => Set<Role>();
    public virtual DbSet<UserRole> UserRoles => Set<UserRole>();
    public virtual DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public virtual DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public virtual DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public virtual DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public virtual DbSet<Question> Questions => Set<Question>();
    public virtual DbSet<QuestionVersion> QuestionVersions => Set<QuestionVersion>();
    public virtual DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public virtual DbSet<Exam> Exams => Set<Exam>();
    public virtual DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();
    public virtual DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();
    public virtual DbSet<AttemptQuestion> AttemptQuestions => Set<AttemptQuestion>();
    public virtual DbSet<AttemptAnswer> AttemptAnswers => Set<AttemptAnswer>();
    public virtual DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();

            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuestionVersion>(entity =>
        {
            entity.HasOne(qv => qv.Question)
                .WithMany(q => q.Versions)
                .HasForeignKey(qv => qv.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(qv => qv.ApprovedBy)
                .WithMany()
                .HasForeignKey(qv => qv.ApprovedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<QuestionOption>(entity =>
        {
            entity.HasOne(qo => qo.QuestionVersion)
                .WithMany(qv => qv.Options)
                .HasForeignKey(qo => qo.QuestionVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExamQuestion>(entity =>
        {
            entity.HasOne(eq => eq.Exam)
                .WithMany(e => e.ExamQuestions)
                .HasForeignKey(eq => eq.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(eq => eq.QuestionVersion)
                .WithMany()
                .HasForeignKey(eq => eq.QuestionVersionId)
                .OnDelete(DeleteBehavior.Restrict); // Important: don't cascade delete questions if exam deleted
        });

        modelBuilder.Entity<ExamAttempt>(entity =>
        {
            entity.HasOne(ea => ea.Exam)
                .WithMany()
                .HasForeignKey(ea => ea.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ea => ea.User)
                .WithMany()
                .HasForeignKey(ea => ea.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AttemptQuestion>(entity =>
        {
            entity.HasOne(aq => aq.ExamAttempt)
                .WithMany(ea => ea.AttemptQuestions)
                .HasForeignKey(aq => aq.ExamAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(aq => aq.QuestionVersion)
                .WithMany()
                .HasForeignKey(aq => aq.QuestionVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AttemptAnswer>(entity =>
        {
            entity.HasOne(aa => aa.AttemptQuestion)
                .WithMany(aq => aq.Answers)
                .HasForeignKey(aa => aa.AttemptQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = RoleType.SuperAdmin },
            new Role { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = RoleType.QuestionCreator },
            new Role { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = RoleType.ExamFacer }
        );
    }
}
