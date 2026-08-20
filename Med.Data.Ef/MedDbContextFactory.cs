using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Med.Data.Ef;

public class MedDbContextFactory : IDesignTimeDbContextFactory<MedDbContext>
{
    public MedDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("MED_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=medsustav;Username=med;Password=med123";

        var options = new DbContextOptionsBuilder<MedDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new MedDbContext(options);
    }
}