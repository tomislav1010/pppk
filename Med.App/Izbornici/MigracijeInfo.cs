using Med.Data.Ef;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace Med.App.Izbornici;

/// Prikazuje stvarno stanje migracija u bazi i objasnjava kad ih nije moguce izvrsiti.
public class MigracijeInfo
{
    private readonly MedDbContext _db;

    public MigracijeInfo(MedDbContext db) => _db = db;

    public async Task PokreniAsync()
    {
        Ui.Naslov("Migracije");

        await StanjeAsync();
        AnsiConsole.WriteLine();
        Objasnjenje();

        Ui.Pauza();
    }

    private async Task StanjeAsync()
    {
        try
        {
            var primijenjene = (await _db.Database.GetAppliedMigrationsAsync()).ToList();
            var neprimijenjene = (await _db.Database.GetPendingMigrationsAsync()).ToList();

            var tablica = new Table().Border(TableBorder.Rounded);
            tablica.AddColumn("Migracija");
            tablica.AddColumn("Stanje");

            foreach (var m in primijenjene)
                tablica.AddRow(m, "[green]primijenjena[/]");

            foreach (var m in neprimijenjene)
                tablica.AddRow(m, "[yellow]ceka[/]");

            if (primijenjene.Count == 0 && neprimijenjene.Count == 0)
                tablica.AddRow("-", "[grey]nema migracija[/]");

            AnsiConsole.Write(tablica);

            Ui.Info($"Primijenjene se vode u tablici __EFMigrationsHistory ({primijenjene.Count}).");

            if (neprimijenjene.Count == 0)
                Ui.Uspjeh("Shema baze odgovara modelu.");
            else
                Ui.Greska($"Neprimijenjenih migracija: {neprimijenjene.Count}.");
        }
        catch (Exception e)
        {
            Ui.Greska("Stanje migracija nije moguce procitati.");
            Ui.Info(e.Message);
        }
    }

    private static void Objasnjenje()
    {
        AnsiConsole.Write(new Rule("[yellow]Kad migraciju nije moguce izvrsiti[/]").LeftJustified());

        var stavke = new (string Naslov, string Tekst)[]
        {
            ("Gubitak podataka",
             "Brisanje stupca ili tablice s podacima, ili suzavanje tipa (varchar(100) -> varchar(20)). " +
             "EF generira upozorenje, a Postgres odbija ako se vrijednosti ne daju pretvoriti."),

            ("Novi NOT NULL stupac bez zadane vrijednosti",
             "Postojeci redci nemaju sto upisati. Rjesenje je DEFAULT, ili migracija u tri koraka: " +
             "dodaj stupac kao nullable, popuni ga, pa tek onda postavi NOT NULL."),

            ("Naknadno dodan UNIQUE nad stupcem s duplikatima",
             "Stvaranje indeksa pada dok se postojeci duplikati ne razrijese."),

            ("Strani kljuc nad podacima koji ga krse",
             "Ako u tablici postoje redci bez odgovarajuceg roditelja, dodavanje FK ogranicenja pada."),

            ("Nekompatibilna promjena tipa",
             "text -> integer prolazi samo ako se svaka vrijednost da pretvoriti. " +
             "Inace treba USING izraz ili prijelazni stupac."),

            ("Razidena povijest",
             "Ako je netko rucno mijenjao shemu, ili je migracija obrisana iz koda a ostala u " +
             "__EFMigrationsHistory, EF vise ne zna od cega racuna razliku."),
        };

        foreach (var (naslov, tekst) in stavke)
        {
            AnsiConsole.MarkupLine($"\n[yellow]{naslov}[/]");
            AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(tekst)}[/]");
        }

        AnsiConsole.WriteLine();
        Ui.Info("Down metoda svake migracije omogucuje povratak unatrag, ali samo ako " +
                "promjena nije nepovratno unistila podatke.");
    }
}
