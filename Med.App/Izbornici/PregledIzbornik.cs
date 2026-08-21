using Med.App.Servisi;
using Med.Domain.Entities;
using Spectre.Console;

namespace Med.App.Izbornici;

public class PregledIzbornik
{
    private readonly PregledService _pregledi;
    private readonly Odabir _odabir;

    public PregledIzbornik(PregledService pregledi, Odabir odabir)
    {
        _pregledi = pregledi;
        _odabir = odabir;
    }

    public async Task PokreniAsync()
    {
        while (true)
        {
            Ui.Naslov("Pregledi");

            var izbor = Ui.Odaberi("Odaberi radnju:", new[]
            {
                "Nadolazeci pregledi",
                "Prikaz za pacijenta",
                "Zakazi pregled",
                "Upisi nalaz / promijeni status",
                "Obrisi pregled",
                "Natrag"
            }, x => x);

            switch (izbor)
            {
                case "Nadolazeci pregledi": await NadolazeciAsync(); break;
                case "Prikaz za pacijenta": await PrikazAsync(); break;
                case "Zakazi pregled": await ZakaziAsync(); break;
                case "Upisi nalaz / promijeni status": await StatusAsync(); break;
                case "Obrisi pregled": await ObrisiAsync(); break;
                default: return;
            }
        }
    }

    private async Task NadolazeciAsync()
    {
        Ui.Naslov("Nadolazeci pregledi");

        var lista = await _pregledi.DohvatiNadolazeceAsync();
        if (lista.Count == 0)
        {
            Ui.Info("Nema nadolazecih pregleda.");
            Ui.Pauza();
            return;
        }

        var tablica = new Table().Border(TableBorder.Rounded);
        tablica.AddColumn("Id");
        tablica.AddColumn("Termin");
        tablica.AddColumn("Tip");
        tablica.AddColumn("Pacijent");
        tablica.AddColumn("Lijecnik");

        foreach (var p in lista)
            tablica.AddRow(
                p.Id.ToString(),
                p.Termin.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                p.Tip.ToString(),
                p.Pacijent is null ? "-" : $"{p.Pacijent.Prezime}, {p.Pacijent.Ime}",
                p.Lijecnik?.ToString() ?? "-");

        AnsiConsole.Write(tablica);
        Ui.Pauza();
    }

    private async Task PrikazAsync()
    {
        Ui.Naslov("Pregledi pacijenta");

        var pacijent = await _odabir.PacijentAsync();
        if (pacijent is null) return;

        Ispisi(await _pregledi.DohvatiZaPacijentaAsync(pacijent.Id));
        Ui.Pauza();
    }

    private static void Ispisi(List<Pregled> lista)
    {
        if (lista.Count == 0)
        {
            Ui.Info("Nema pregleda.");
            return;
        }

        var tablica = new Table().Border(TableBorder.Rounded);
        tablica.AddColumn("Id");
        tablica.AddColumn("Termin");
        tablica.AddColumn("Tip");
        tablica.AddColumn("Trajanje");
        tablica.AddColumn("Status");
        tablica.AddColumn("Specijalist");
        tablica.AddColumn("Uputitelj");
        tablica.AddColumn("Nalaz");

        foreach (var p in lista)
            tablica.AddRow(
                p.Id.ToString(),
                p.Termin.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                p.Tip.ToString(),
                $"{p.TrajanjeMinuta} min",
                p.Status.ToString(),
                p.Lijecnik?.ToString() ?? "-",
                p.Uputitelj?.ToString() ?? "-",
                string.IsNullOrWhiteSpace(p.Nalaz) ? "-" : p.Nalaz);

        AnsiConsole.Write(tablica);
    }

    private async Task ZakaziAsync()
    {
        Ui.Naslov("Zakazivanje pregleda");

        var pacijent = await _odabir.PacijentAsync();
        if (pacijent is null) return;

        var specijalist = await _odabir.LijecnikAsync("Lijecnik specijalist:");
        if (specijalist is null) return;

        int? uputiteljId = null;
        if (Ui.Potvrdi("Postoji lijecnik uputitelj?"))
        {
            var uputitelj = await _odabir.LijecnikAsync("Lijecnik uputitelj:");
            uputiteljId = uputitelj?.Id;
        }

        var pregled = new Pregled
        {
            PacijentId = pacijent.Id,
            LijecnikId = specijalist.Id,
            UputiteljId = uputiteljId,
            Tip = Ui.Odaberi("Tip pregleda:", Enum.GetValues<TipPregleda>(), t => t.ToString()),
            Termin = Ui.Termin("Termin:"),
            TrajanjeMinuta = Ui.Broj("Trajanje (min):", 30)
        };

        var (uspjeh, poruka) = await _pregledi.ZakaziAsync(pregled);
        if (uspjeh) Ui.Uspjeh(poruka); else Ui.Greska(poruka);
        Ui.Pauza();
    }

    private async Task StatusAsync()
    {
        Ui.Naslov("Status pregleda");

        var pregled = await OdaberiAsync();
        if (pregled is null) return;

        var status = Ui.Odaberi("Novi status:", Enum.GetValues<StatusPregleda>(), s => s.ToString());
        string? nalaz = null;

        if (status == StatusPregleda.Odrzan)
            nalaz = Ui.TekstOpcionalno("Nalaz:", pregled.Nalaz);

        await _pregledi.PromijeniStatusAsync(pregled.Id, status, nalaz);
        Ui.Uspjeh("Pregled azuriran.");
        Ui.Pauza();
    }

    private async Task ObrisiAsync()
    {
        Ui.Naslov("Brisanje pregleda");

        var pregled = await OdaberiAsync();
        if (pregled is null) return;

        if (!Ui.Potvrdi("Sigurno obrisati pregled?")) return;

        await _pregledi.ObrisiAsync(pregled.Id);
        Ui.Uspjeh("Pregled obrisan.");
        Ui.Pauza();
    }

    private async Task<Pregled?> OdaberiAsync()
    {
        var pacijent = await _odabir.PacijentAsync();
        if (pacijent is null) return null;

        var lista = await _pregledi.DohvatiZaPacijentaAsync(pacijent.Id);
        if (lista.Count == 0)
        {
            Ui.Info("Pacijent nema pregleda.");
            Ui.Pauza();
            return null;
        }

        var odabrani = Ui.Odaberi("Odaberi pregled:", lista,
            p => $"{p.Tip} {p.Termin.ToLocalTime():dd.MM.yyyy HH:mm} ({p.Status})");

        return await _pregledi.DohvatiAsync(odabrani.Id);
    }
}
