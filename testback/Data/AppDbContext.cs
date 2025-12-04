using Microsoft.EntityFrameworkCore;
using testback.Models;

namespace testback.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UserId).HasColumnName("UserId");
                entity.Property(e => e.Username).HasColumnName("Username").IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).HasColumnName("PasswordHash").IsRequired().HasMaxLength(255);
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
                entity.Property(e => e.IsActive).HasColumnName("IsActive");
            });
        }
    }
}
