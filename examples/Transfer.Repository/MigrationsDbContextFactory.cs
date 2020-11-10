using Microsoft.EntityFrameworkCore.Design;

namespace Transfer.Repository
{
    public class MigrationsDbContextFactory : IDesignTimeDbContextFactory<TransferDbContext>
    {
        public TransferDbContext CreateDbContext(string[] args)
        {
            return new TransferDbContext();
        }
    }
}
