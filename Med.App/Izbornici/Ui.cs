using Spectre.Console;

namespace Med.App.Izbornici;

public static class Ui
{
    public static void Naslov(string tekst)
    {
        Ocisti();
        AnsiConsole.Write(new Rule($"[yellow]{tekst}[/]").LeftJustified());
        AnsiConsole.WriteLine();
    }

    // Clear() cita poziciju kursora i puca kad izlaz nije terminal (preusmjeren u datoteku).
    public static void Ocisti()
    {
        try { AnsiConsole.Clear(); }
        catch (IOException) { AnsiConsole.WriteLine(); }
    }

    public static void Uspjeh(string poruka) => AnsiConsole.MarkupLine($"[green]{poruka}[/]");

    public static void Greska(string poruka) => AnsiConsole.MarkupLine($"[red]{poruka}[/]");

    public static void Info(string poruka) => AnsiConsole.MarkupLine($"[grey]{poruka}[/]");

    public static void Pauza()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Markup("[grey]Enter za nastavak...[/]");
        Console.ReadLine();
    }

    public static bool Potvrdi(string pitanje) => AnsiConsole.Confirm(pitanje, false);

    public static string Tekst(string oznaka, string? zadano = null)
    {
        var upit = new TextPrompt<string>(oznaka);
        if (zadano is not null) upit.DefaultValue(zadano);
        return AnsiConsole.Prompt(upit).Trim();
    }

    public static string? TekstOpcionalno(string oznaka, string? zadano = null)
    {
        var upit = new TextPrompt<string>(oznaka).AllowEmpty();
        if (zadano is not null) upit.DefaultValue(zadano);
        var vrijednost = AnsiConsole.Prompt(upit).Trim();
        return string.IsNullOrWhiteSpace(vrijednost) ? null : vrijednost;
    }

    public static int Broj(string oznaka, int? zadano = null)
    {
        var upit = new TextPrompt<int>(oznaka);
        if (zadano is not null) upit.DefaultValue(zadano.Value);
        return AnsiConsole.Prompt(upit);
    }

    public static decimal Decimalni(string oznaka, decimal? zadano = null)
    {
        var upit = new TextPrompt<decimal>(oznaka);
        if (zadano is not null) upit.DefaultValue(zadano.Value);
        return AnsiConsole.Prompt(upit);
    }

    public static double? DecimalniOpcionalno(string oznaka, double? zadano = null)
    {
        var tekst = TekstOpcionalno(oznaka, zadano?.ToString());
        if (tekst is null) return null;
        return double.TryParse(tekst, out var vrijednost) ? vrijednost : null;
    }

    // Npgsql odbija DateTime s Kind=Local za "timestamp without time zone",
    // pa svaki uneseni datum eksplicitno oznacavamo kao Unspecified.
    public static DateTime Datum(string oznaka, DateTime? zadano = null)
    {
        while (true)
        {
            var tekst = Tekst($"{oznaka} [grey](dd.MM.yyyy)[/]", zadano?.ToString("dd.MM.yyyy"));
            if (DateTime.TryParseExact(tekst, "dd.MM.yyyy", null,
                    System.Globalization.DateTimeStyles.None, out var datum))
                return DateTime.SpecifyKind(datum, DateTimeKind.Unspecified);

            Greska("Neispravan format. Primjer: 12.03.1985");
        }
    }

    public static DateTime? DatumOpcionalno(string oznaka, DateTime? zadano = null)
    {
        while (true)
        {
            var tekst = TekstOpcionalno($"{oznaka} [grey](dd.MM.yyyy, prazno = bez datuma)[/]",
                zadano?.ToString("dd.MM.yyyy"));
            if (tekst is null) return null;

            if (DateTime.TryParseExact(tekst, "dd.MM.yyyy", null,
                    System.Globalization.DateTimeStyles.None, out var datum))
                return DateTime.SpecifyKind(datum, DateTimeKind.Unspecified);

            Greska("Neispravan format. Primjer: 12.03.1985");
        }
    }

    // Npgsql prima samo offset 0 za "timestamp with time zone", pa lokalno
    // uneseno vrijeme odmah pretvaramo u UTC. Prikaz ide kroz ToLocalTime().
    public static DateTimeOffset Termin(string oznaka, DateTimeOffset? zadano = null)
    {
        while (true)
        {
            var tekst = Tekst($"{oznaka} [grey](dd.MM.yyyy HH:mm)[/]",
                zadano?.ToLocalTime().ToString("dd.MM.yyyy HH:mm"));

            if (DateTime.TryParseExact(tekst, "dd.MM.yyyy HH:mm", null,
                    System.Globalization.DateTimeStyles.None, out var vrijeme))
                return new DateTimeOffset(DateTime.SpecifyKind(vrijeme, DateTimeKind.Local))
                    .ToUniversalTime();

            Greska("Neispravan format. Primjer: 05.09.2026 14:30");
        }
    }

    public static T Odaberi<T>(string oznaka, IEnumerable<T> stavke, Func<T, string> prikaz)
        where T : notnull =>
        AnsiConsole.Prompt(
            new SelectionPrompt<T>()
                .Title(oznaka)
                .PageSize(15)
                .MoreChoicesText("[grey](strelice za vise stavki)[/]")
                .UseConverter(prikaz)
                .AddChoices(stavke));
}
