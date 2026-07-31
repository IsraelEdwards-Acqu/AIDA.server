using AIDA.Server.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace AIDA.Server.Data
{
    public class AidaDbContext : DbContext
    {
        public AidaDbContext(DbContextOptions<AidaDbContext> options) : base(options) { }

        public DbSet<Student> Students { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<AdminResponse> AdminResponses { get; set; }
        public DbSet<KnowledgeEntry> KnowledgeBase { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ✅ Seed a default admin account
            var adminPassword = BCrypt.Net.BCrypt.HashPassword("Admin@123");

            modelBuilder.Entity<Admin>().HasData(
                new Admin
                {
                    AdminId = 1,
                    Username = "admin",
                    PasswordHash = adminPassword,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }
}
