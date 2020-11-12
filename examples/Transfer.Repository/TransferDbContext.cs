using Microsoft.EntityFrameworkCore;
using Transfer.Repository.Entities;

namespace Transfer.Repository
{
    public class TransferDbContext : DbContext
    {
        public TransferDbContext()
        {
            this.Database.EnsureCreated();
            this.Database.Migrate();
        }

        public DbSet<Account> Accounts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Server=172.16.4.102;Port=5432;Database=Vertex;User Id=postgres;Password=postgres;Pooling=true;MaxPoolSize=20;");
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>().Property(x => x.Id).ValueGeneratedNever();
            base.OnModelCreating(modelBuilder);
        }
    }
}