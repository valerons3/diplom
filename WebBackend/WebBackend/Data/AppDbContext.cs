using Microsoft.EntityFrameworkCore;
using WebBackend.Models.Entity;

namespace WebBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        
        public DbSet<User> Users { get; set; }
        public DbSet<ProcessedData> ProccesedDatas { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<RevokedToken> RevokedTokens { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Statistic> Statistics { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Настройка Users
            modelBuilder.Entity<User>()
                .HasOne(u => u.UserRole)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId);

            modelBuilder.Entity<User>()
                .HasOne(u => u.UserRefreshToken)
                .WithOne(rt => rt.User)
                .HasForeignKey<RefreshToken>(rt => rt.UserId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.UserProcessedData)
                .WithOne()
                .HasForeignKey(s => s.UserId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.UserStatistics)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId);


            // Настройка ProcessedDatas
            modelBuilder.Entity<ProcessedData>()
                .HasOne(pr => pr.User)
                .WithMany(u => u.UserProcessedData)
                .HasForeignKey(pr => pr.UserId);

            modelBuilder.Entity<ProcessedData>()
                .HasOne(pr => pr.Rating)
                .WithOne(r => r.ProccesedData)
                .HasForeignKey<ProcessedData>(pr => pr.RatingId);

            modelBuilder.Entity<ProcessedData>()
                .Property(p => p.Status)
                .HasConversion<string>();

            // Настройка Ratings
            modelBuilder.Entity<Rating>()
                .HasOne(r => r.ProccesedData)
                .WithOne(pr => pr.Rating)
                .HasForeignKey<ProcessedData>(pr => pr.RatingId);


            // Настройка RefreshTokens
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithOne(u => u.UserRefreshToken)
                .HasForeignKey<RefreshToken>(rt => rt.UserId);

            // Настройка Roles
            modelBuilder.Entity<Role>()
                .HasMany(r => r.Users)
                .WithOne(u => u.UserRole)
                .HasForeignKey(u => u.RoleId);

            // Настройка Statistics
            modelBuilder.Entity<Statistic>()
                .HasOne(s => s.User)
                .WithMany(u => u.UserStatistics)
                .HasForeignKey(s => s.UserId);

            modelBuilder.Entity<Role>().HasData(
                 new Role { Id = new Guid("f47c7b39-dde9-49c5-87b0-c1a3d20932e0"), Name = "User" },
                 new Role { Id = new Guid("b2c81b19-b3c5-4274-9173-cc8f4e87c574"), Name = "Admin" }
            );
        }
    }
}