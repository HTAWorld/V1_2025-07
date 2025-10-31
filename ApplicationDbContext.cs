using Microsoft.EntityFrameworkCore;
using System.Numerics;
using V1_2025_07.Models;

namespace V1_2025_07
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Match> Matches { get; set; }

        public DbSet<Player> Players { get; set; }
        public DbSet<Rule> Rules { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }



    }

}
