using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace UserManagement.Models.Data
{
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base (options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<EmailVerification> EmailVerifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId });

                entity.HasOne(ur => ur.User)
                      .WithMany(u => u.UserRoles)
                      .HasForeignKey(ur => ur.UserId);

                entity.HasOne(ur => ur.Role)
                      .WithMany(r => r.UserRoles)
                      .HasForeignKey(ur => ur.RoleId);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.FirstName)
                        .IsRequired()
                        .HasMaxLength(100);
                entity.Property(e => e.LastName)
                        .IsRequired()
                        .HasMaxLength(100);
                entity.Property(e => e.Email)
                        .IsRequired()
                        .HasMaxLength(200);
                entity.HasIndex(e => e.Email)
                        .IsUnique();
                entity.Property(e => e.PasswordHash)
                        .IsRequired();
                entity.Property(e => e.IsActive)
                        .IsRequired();
                entity.Property(e => e.CreatedAt)
                        .IsRequired();
                entity.Property(e => e.UpdatedAt);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.RoleId);
                entity.Property(e => e.RoleName)
                        .IsRequired()
                        .HasMaxLength(100);
            });

            modelBuilder.Entity<EmailVerification>(entity =>
            {
                entity.HasKey(e => e.VerificationId);
                entity.Property(e => e.Email)
                        .IsRequired();
                entity.Property(e => e.VerificationCode)
                        .IsRequired()
                        .HasMaxLength(100);
                entity.Property(e => e.IsVerified)
                        .IsRequired();
                entity.Property(e => e.ExpirationTime)
                        .IsRequired();
            });

        }
    
    }
}
