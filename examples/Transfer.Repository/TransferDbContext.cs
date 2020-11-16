using Microsoft.EntityFrameworkCore;
using Transfer.Repository.Entities;

namespace Transfer.Repository
{
    public sealed class TransferDbContext : DbContext
    {
        public static string ConnectionString { get; set; }
        public TransferDbContext()
        {
            this.Database.EnsureCreated();
            this.Database.Migrate();
        }

        public DbSet<Account> Accounts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(ConnectionString);
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>().Property(x => x.Id).ValueGeneratedNever();
            base.OnModelCreating(modelBuilder);
        }
    }
}