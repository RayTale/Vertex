using Microsoft.EntityFrameworkCore;
using Transfer.Repository.Entities;

namespace Transfer.Repository
{
    public class TransferDbContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Server=172.16.4.102;Port=5432;Database=Vertex;User Id=postgres;Password=postgres;Pooling=true;MaxPoolSize=20;");
            base.OnConfiguring(optionsBuilder);
        }
    }
}