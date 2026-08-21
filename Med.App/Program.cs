using Med.App;
using Med.App.Izbornici;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var config = Baza.UcitajKonfiguraciju();
Baza.IspisiSql = args.Contains("--sql");

await using var db = Baza.Otvori(config);

AnsiConsole.MarkupLine($"[grey]Konfiguracija:[/] {Baza.NazivKonfiguracije(config)}");

if (!AnsiConsole.Profile.Capabilities.Interactive)
{
    AnsiConsole.MarkupLine("[red]Izbornik trazi interaktivni terminal.[/]");
    AnsiConsole.MarkupLine("[grey]Pokreni aplikaciju izravno u konzoli, bez preusmjeravanja ulaza.[/]");
    return 1;
}

if (!await db.Database.CanConnectAsync())
{
    AnsiConsole.MarkupLine("[red]Baza nije dostupna.[/]");
    AnsiConsole.MarkupLine("[grey]Pokreni kontejner: docker compose up -d postgres[/]");
    return 1;
}

var neprimijenjene = (await db.Database.GetPendingMigrationsAsync()).ToList();
if (neprimijenjene.Count > 0)
{
    AnsiConsole.MarkupLine($"[yellow]Neprimijenjene migracije: {string.Join(", ", neprimijenjene)}[/]");
    if (AnsiConsole.Confirm("Primijeniti sada?"))
        await db.Database.MigrateAsync();
    else
        return 1;
}

await SeedPodataka.PokreniAsync(db);

await new GlavniIzbornik(config, db).PokreniAsync();

AnsiConsole.MarkupLine("[grey]Dovidenja.[/]");
return 0;
