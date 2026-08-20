using Med.App;
using Med.Data.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Local.json", optional: true)
    .Build();

var kljuc = config["UseConnection"] ?? "Postgres";
var connectionString = config.GetConnectionString(kljuc)
    ?? throw new InvalidOperationException($"Nema connection stringa '{kljuc}'.");

var options = new DbContextOptionsBuilder<MedDbContext>()
    .UseNpgsql(connectionString)
    .Options;

await using var db = new MedDbContext(options);

Console.WriteLine($"Konfiguracija : {kljuc}");
Console.WriteLine($"Dostupna baza : {await db.Database.CanConnectAsync()}");

await Seeder.PokreniAsync(db);

Console.WriteLine("\nLijecnici:");
foreach (var l in await db.Lijecnici.AsNoTracking().ToListAsync())
    Console.WriteLine($"  {l}");
