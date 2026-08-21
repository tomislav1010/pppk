using Med.Data.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Med.App;

public static class Baza
{
    public static bool IspisiSql { get; set; }

    public static IConfiguration UcitajKonfiguraciju() =>
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .Build();

    public static string NazivKonfiguracije(IConfiguration config) =>
        config["UseConnection"] ?? "Postgres";

    public static MedDbContext Otvori(
        IConfiguration config,
        bool lazyLoading = false,
        Action<string>? sqlSink = null)
    {
        var kljuc = NazivKonfiguracije(config);
        var cs = config.GetConnectionString(kljuc)
            ?? throw new InvalidOperationException($"Nema connection stringa '{kljuc}'.");

        var builder = new DbContextOptionsBuilder<MedDbContext>().UseNpgsql(cs);

        if (lazyLoading)
            builder.UseLazyLoadingProxies();

        var odrediste = sqlSink ?? (IspisiSql ? Console.WriteLine : null);
        if (odrediste is not null)
            builder.LogTo(odrediste, new[] { RelationalEventId.CommandExecuted }, LogLevel.Information);

        return new MedDbContext(builder.Options);
    }
}
