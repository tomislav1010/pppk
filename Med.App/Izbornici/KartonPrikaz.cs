using Med.App.Servisi;
using Spectre.Console;

namespace Med.App.Izbornici;

public class KartonPrikaz
{
    private readonly PacijentService _pacijenti;
    private readonly Odabir _odabir;

    public KartonPrikaz(PacijentService pacijenti, Odabir odabir)
    {
        _pacijenti = pacijenti;
        _odabir = odabir;
    }

    public async Task PokreniAsync()
    {
        Ui.Naslov("Puni karton pacijenta");

        var odabrani = await _odabir.PacijentAsync();
        if (odabrani is null) return;

        var p = await _pacijenti.DohvatiPunAsync(odabrani.Id);
        if (p is null)
        {
            Ui.Greska("Pacijent nije pronaden.");
            Ui.Pauza();
            return;
        }

        Ui.Naslov($"Karton: {p.Prezime}, {p.Ime}");

        var osnovno = new Grid();
        osnovno.AddColumn(new GridColumn().Width(22));
        osnovno.AddColumn();
        osnovno.AddRow("OIB", p.Oib);
        osnovno.AddRow("Datum rodenja", p.DatumRodenja.ToString("dd.MM.yyyy"));
        osnovno.AddRow("Spol", p.Spol.ToString());
        osnovno.AddRow("Telefon", p.Telefon ?? "-");
        osnovno.AddRow("Adresa boravista", p.AdresaBoravista?.ToString() ?? "-");
        osnovno.AddRow("Adresa prebivalista", p.AdresaPrebivalista?.ToString() ?? "(isto kao boraviste)");
        osnovno.AddRow("U evidenciji od", p.KreiranoNa.ToLocalTime().ToString("dd.MM.yyyy HH:mm"));

        if (p.Karton is not null)
        {
            osnovno.AddRow("Krvna grupa", p.Karton.KrvnaGrupa ?? "-");
            osnovno.AddRow("Visina / tezina",
                $"{p.Karton.VisinaCm?.ToString("0.#") ?? "-"} cm / {p.Karton.TezinaKg?.ToString("0.#") ?? "-"} kg");
            osnovno.AddRow("Alergije", p.Karton.Alergije ?? "-");
        }

        AnsiConsole.Write(new Panel(osnovno)
            .Header("[yellow]Osnovni podaci (1:1 karton, N:1 adrese)[/]")
            .Border(BoxBorder.Rounded));

        AnsiConsole.WriteLine();

        var povijest = new Table().Border(TableBorder.Rounded).Title("[yellow]Povijest bolesti (1:N)[/]");
        povijest.AddColumn("Dijagnoza");
        povijest.AddColumn("Od");
        povijest.AddColumn("Do");
        povijest.AddColumn("Napomena");

        if (p.PovijestBolesti.Count == 0)
            povijest.AddRow("-", "-", "-", "-");
        else
            foreach (var pb in p.PovijestBolesti.OrderByDescending(x => x.DatumOd))
                povijest.AddRow(
                    pb.Dijagnoza?.ToString() ?? "-",
                    pb.DatumOd.ToString("dd.MM.yyyy"),
                    pb.DatumDo?.ToString("dd.MM.yyyy") ?? "u tijeku",
                    pb.Napomena ?? "-");

        AnsiConsole.Write(povijest);

        var terapije = new Table().Border(TableBorder.Rounded).Title("[yellow]Terapije (1:N, lijek N:1)[/]");
        terapije.AddColumn("Lijek");
        terapije.AddColumn("Doza");
        terapije.AddColumn("Ucestalost");
        terapije.AddColumn("Od");
        terapije.AddColumn("Aktivna");

        if (p.Terapije.Count == 0)
            terapije.AddRow("-", "-", "-", "-", "-");
        else
            foreach (var t in p.Terapije.OrderByDescending(x => x.DatumOd))
                terapije.AddRow(
                    t.Lijek?.Naziv ?? "-",
                    $"{t.Doza:0.##} {t.JedinicaDoze}",
                    t.Ucestalost,
                    t.DatumOd.ToString("dd.MM.yyyy"),
                    t.Aktivna ? "da" : "ne");

        AnsiConsole.Write(terapije);

        var pregledi = new Table().Border(TableBorder.Rounded).Title("[yellow]Pregledi (1:N, lijecnik N:1)[/]");
        pregledi.AddColumn("Termin");
        pregledi.AddColumn("Tip");
        pregledi.AddColumn("Status");
        pregledi.AddColumn("Specijalist");
        pregledi.AddColumn("Nalaz");

        if (p.Pregledi.Count == 0)
            pregledi.AddRow("-", "-", "-", "-", "-");
        else
            foreach (var pr in p.Pregledi.OrderByDescending(x => x.Termin))
                pregledi.AddRow(
                    pr.Termin.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                    pr.Tip.ToString(),
                    pr.Status.ToString(),
                    pr.Lijecnik?.ToString() ?? "-",
                    string.IsNullOrWhiteSpace(pr.Nalaz) ? "-" : pr.Nalaz);

        AnsiConsole.Write(pregledi);
        Ui.Pauza();
    }
}
