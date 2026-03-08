using Microsoft.EntityFrameworkCore;
using MunicipalIssueTracker.Domain.Entities;
using MunicipalIssueTracker.Domain.Enums;

namespace MunicipalIssueTracker.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.UserId);
            e.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
            e.Property(u => u.Email).HasMaxLength(200).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        });

        // District
        modelBuilder.Entity<District>(e =>
        {
            e.HasKey(d => d.DistrictId);
            e.Property(d => d.Name).HasMaxLength(100).IsRequired();
        });

        // Category
        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.CategoryId);
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.Property(c => c.Icon).HasMaxLength(50);
            e.Property(c => c.DefaultPriority).HasConversion<string>().HasMaxLength(20);
        });

        // Status
        modelBuilder.Entity<Status>(e =>
        {
            e.HasKey(s => s.StatusId);
            e.Property(s => s.Name).HasMaxLength(50).IsRequired();
        });

        // Issue
        modelBuilder.Entity<Issue>(e =>
        {
            e.HasKey(i => i.IssueId);
            e.Property(i => i.Title).HasMaxLength(200).IsRequired();
            e.Property(i => i.Description).HasMaxLength(4000);
            e.Property(i => i.AddressText).HasMaxLength(300);
            e.Property(i => i.Priority).HasConversion<string>().HasMaxLength(20);

            e.HasOne(i => i.Category).WithMany(c => c.Issues).HasForeignKey(i => i.CategoryId);
            e.HasOne(i => i.Status).WithMany(s => s.Issues).HasForeignKey(i => i.StatusId);
            e.HasOne(i => i.District).WithMany(d => d.Issues).HasForeignKey(i => i.DistrictId);
            e.HasOne(i => i.CreatedBy).WithMany(u => u.CreatedIssues).HasForeignKey(i => i.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.AssignedTo).WithMany(u => u.AssignedIssues).HasForeignKey(i => i.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(i => i.StatusId);
            e.HasIndex(i => i.CategoryId);
            e.HasIndex(i => i.DistrictId);
            e.HasIndex(i => i.AssignedToUserId);
        });

        // Comment
        modelBuilder.Entity<Comment>(e =>
        {
            e.HasKey(c => c.CommentId);
            e.Property(c => c.Body).HasMaxLength(2000).IsRequired();
            e.HasOne(c => c.Issue).WithMany(i => i.Comments).HasForeignKey(c => c.IssueId);
            e.HasOne(c => c.Author).WithMany(u => u.Comments).HasForeignKey(c => c.AuthorUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // Attachment
        modelBuilder.Entity<Attachment>(e =>
        {
            e.HasKey(a => a.AttachmentId);
            e.Property(a => a.FileName).HasMaxLength(255).IsRequired();
            e.Property(a => a.ContentType).HasMaxLength(100);
            e.Property(a => a.StoragePath).HasMaxLength(500).IsRequired();
            e.HasOne(a => a.Issue).WithMany(i => i.Attachments).HasForeignKey(a => a.IssueId);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.AuditId);
            e.Property(a => a.Action).HasMaxLength(100).IsRequired();
            e.Property(a => a.DetailsJson).HasMaxLength(4000);
            e.HasOne(a => a.Issue).WithMany(i => i.AuditLogs).HasForeignKey(a => a.IssueId);
            e.HasOne(a => a.Actor).WithMany(u => u.AuditLogs).HasForeignKey(a => a.ActorUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(a => a.IssueId);
        });
    }
}
