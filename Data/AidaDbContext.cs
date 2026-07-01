using AIDA.Server.Models;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using AIDA.Server.Models;

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
    }
}


