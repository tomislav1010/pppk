using Med.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

namespace Med.App.Izbornici;

public class UcitavanjeDemo
{
    private readonly IConfiguration _config;

    public UcitavanjeDemo(IConfiguration config) => _config = config;

    public async Task PokreniAsync()
    {
        Ui.Naslov("Eager vs lazy loading");

        int pacijentId;
        await using (var db = Baza.Otvori(_config))
        {
            var kandidat = await db.Pacijenti
                .OrderByDescending(p => p.Pregledi.Count + p.Terapije.Count)
                .Select(p => new { p.Id, p.Ime, p.Prezime })
                .FirstOrDefaultAsync();

            if (kandidat is null)
            {
                Ui.Info("Nema pacijenata. Ucitaj demo podatke iz glavnog izbornika.");
                Ui.Pauza();
                return;
            }

            pacijentId = kandidat.Id;
            Ui.Info($"Demonstracija na pacijentu: {kandidat.Prezime}, {kandidat.Ime}\n");
        }

        var eagerUpiti = await EagerAsync(pacijentId);
        var lazyUpiti = await LazyAsync(pacijentId);

        Sazetak(eagerUpiti, lazyUpiti);
        Ui.Pauza();
    }

    private async Task<List<string>> EagerAsync(int pacijentId)
    {
        var upiti = new List<string>();

        AnsiConsole.Write(new Rule("[green]EAGER (Include)[/]").LeftJustified());

        await using var db = Baza.Otvori(_config, lazyLoading: false, sqlSink: s => Zabiljezi(upiti, s));

        var pacijent = await db.Pacijenti
            .Include(p => p.AdresaBoravista)
            .Include(p => p.Karton)
            .Include(p => p.PovijestBolesti).ThenInclude(pb => pb.Dijagnoza)
            .Include(p => p.Terapije).ThenInclude(t => t.Lijek)
            .Include(p => p.Pregledi).ThenInclude(pr => pr.Lijecnik)
            .FirstAsync(p => p.Id == pacijentId);

        Dodirni(pacijent);

        AnsiConsole.MarkupLine($"Izvrseno SQL upita: [yellow]{upiti.Count}[/]");
        Ui.Info("Sve povezane tablice dohvacene su unaprijed, u jednom prolazu.");
        AnsiConsole.WriteLine();

        return upiti;
    }

    private async Task<List<string>> LazyAsync(int pacijentId)
    {
        var upiti = new List<string>();

        AnsiConsole.Write(new Rule("[blue]LAZY (proxy)[/]").LeftJustified());

        await using var db = Baza.Otvori(_config, lazyLoading: true, sqlSink: s => Zabiljezi(upiti, s));

        var pacijent = await db.Pacijenti.FirstAsync(p => p.Id == pacijentId);
        AnsiConsole.MarkupLine($"  nakon dohvata pacijenta: [yellow]{upiti.Count}[/] upit(a)");

        _ = pacijent.AdresaBoravista?.Grad;
        AnsiConsole.MarkupLine($"  nakon .AdresaBoravista:  [yellow]{upiti.Count}[/] upit(a)");

        _ = pacijent.Karton?.KrvnaGrupa;
        AnsiConsole.MarkupLine($"  nakon .Karton:           [yellow]{upiti.Count}[/] upit(a)");

        _ = pacijent.PovijestBolesti.Count;
        AnsiConsole.MarkupLine($"  nakon .PovijestBolesti:  [yellow]{upiti.Count}[/] upit(a)");

        // svaki pristup .Dijagnoza unutar petlje okida zaseban SELECT - to je N+1
        foreach (var pb in pacijent.PovijestBolesti)
            _ = pb.Dijagnoza?.Naziv;
        AnsiConsole.MarkupLine($"  nakon .Dijagnoza u petlji: [yellow]{upiti.Count}[/] upit(a)");

        _ = pacijent.Terapije.Count;
        foreach (var t in pacijent.Terapije)
            _ = t.Lijek?.Naziv;
        AnsiConsole.MarkupLine($"  nakon .Terapije + .Lijek: [yellow]{upiti.Count}[/] upit(a)");

        _ = pacijent.Pregledi.Count;
        foreach (var pr in pacijent.Pregledi)
            _ = pr.Lijecnik?.Prezime;
        AnsiConsole.MarkupLine($"  nakon .Pregledi + .Lijecnik: [yellow]{upiti.Count}[/] upit(a)");

        AnsiConsole.WriteLine();
        return upiti;
    }

    private static void Dodirni(Pacijent pacijent)
    {
        _ = pacijent.AdresaBoravista?.Grad;
        _ = pacijent.Karton?.KrvnaGrupa;
        foreach (var pb in pacijent.PovijestBolesti) _ = pb.Dijagnoza?.Naziv;
        foreach (var t in pacijent.Terapije) _ = t.Lijek?.Naziv;
        foreach (var pr in pacijent.Pregledi) _ = pr.Lijecnik?.Prezime;
    }

    private static void Zabiljezi(List<string> upiti, string zapis)
    {
        var redak = zapis.Split('\n')
            .FirstOrDefault(r => r.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase));

        upiti.Add(redak?.Trim() ?? zapis.Trim());
    }

    private static void Sazetak(List<string> eager, List<string> lazy)
    {
        AnsiConsole.Write(new Rule("[yellow]Zakljucak[/]").LeftJustified());

        var tablica = new Table().Border(TableBorder.Rounded);
        tablica.AddColumn("Pristup");
        tablica.AddColumn("Broj SQL upita");
        tablica.AddColumn("Nedostatak");

        tablica.AddRow(
            "Eager (Include)",
            eager.Count.ToString(),
            "Jedan veliki JOIN dohvaca i podatke koji mozda nisu potrebni;\nkod vise 1:N grana redci se umnazaju (kartezijeva eksplozija).");

        tablica.AddRow(
            "Lazy (proxy)",
            lazy.Count.ToString(),
            "Pocetni upit je malen, ali svaki pristup navigaciji okida novi\nSELECT - u petlji to daje N+1 problem i mrezni round-trip po retku.");

        AnsiConsole.Write(tablica);

        if (lazy.Count > eager.Count)
            AnsiConsole.MarkupLine(
                $"\nLazy je izveo [red]{lazy.Count - eager.Count}[/] upita vise za isti skup podataka.");

        AnsiConsole.WriteLine();
        Ui.Info("Prvih nekoliko SQL naredbi (lazy):");
        foreach (var upit in lazy.Take(6))
            AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(Skrati(upit))}[/]");
    }

    private static string Skrati(string tekst) =>
        tekst.Length <= 110 ? tekst : tekst[..110] + " ...";
}
