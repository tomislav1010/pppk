using Med.App.Servisi;
using Med.Domain.Entities;
using Spectre.Console;

namespace Med.App.Izbornici;

public class PovijestBolestiIzbornik
{
    private readonly PovijestBolestiService _povijest;
    private readonly Odabir _odabir;

    public PovijestBolestiIzbornik(PovijestBolestiService povijest, Odabir odabir)
    {
        _povijest = povijest;
        _odabir = odabir;
    }

    public async Task PokreniAsync()
    {
        while (true)
        {
            Ui.Naslov("Povijest bolesti");

            var izbor = Ui.Odaberi("Odaberi radnju:", new[]
            {
                "Prikaz za pacijenta",
                "Novi zapis",
                "Uredi zapis",
                "Obrisi zapis",
                "Natrag"
            }, x => x);

            switch (izbor)
            {
                case "Prikaz za pacijenta": await PrikazAsync(); break;
                case "Novi zapis": await NoviAsync(); break;
                case "Uredi zapis": await UrediAsync(); break;
                case "Obrisi zapis": await ObrisiAsync(); break;
                default: return;
            }
        }
    }

    private async Task PrikazAsync()
    {
        Ui.Naslov("Povijest bolesti pacijenta");

        var pacijent = await _odabir.PacijentAsync();
        if (pacijent is null) return;

        Ispisi(await _povijest.DohvatiZaPacijentaAsync(pacijent.Id));
        Ui.Pauza();
    }

    private static void Ispisi(List<PovijestBolesti> lista)
    {
        if (lista.Count == 0)
        {
            Ui.Info("Nema zapisa povijesti bolesti.");
            return;
        }

        var tablica = new Table().Border(TableBorder.Rounded);
        tablica.AddColumn("Id");
        tablica.AddColumn("Dijagnoza");
        tablica.AddColumn("Od");
        tablica.AddColumn("Do");
        tablica.AddColumn("Lijecnik");
        tablica.AddColumn("Napomena");

        foreach (var pb in lista)
            tablica.AddRow(
                pb.Id.ToString(),
                pb.Dijagnoza?.ToString() ?? "-",
                pb.DatumOd.ToString("dd.MM.yyyy"),
                pb.DatumDo?.ToString("dd.MM.yyyy") ?? "u tijeku",
                pb.Lijecnik?.ToString() ?? "-",
                pb.Napomena ?? "-");

        AnsiConsole.Write(tablica);
    }

    private async Task NoviAsync()
    {
        Ui.Naslov("Novi zapis povijesti bolesti");

        var pacijent = await _odabir.PacijentAsync();
        if (pacijent is null) return;

        var dijagnoza = await _odabir.DijagnozaAsync();
        if (dijagnoza is null) return;

        var lijecnik = await _odabir.LijecnikAsync();
        if (lijecnik is null) return;

        var zapis = new PovijestBolesti
        {
            PacijentId = pacijent.Id,
            DijagnozaId = dijagnoza.Id,
            LijecnikId = lijecnik.Id,
            DatumOd = Ui.Datum("Datum pocetka:"),
            DatumDo = Ui.DatumOpcionalno("Datum zavrsetka:"),
            Napomena = Ui.TekstOpcionalno("Napomena:")
        };

        var (uspjeh, poruka) = await _povijest.DodajAsync(zapis);
        if (uspjeh) Ui.Uspjeh(poruka); else Ui.Greska(poruka);
        Ui.Pauza();
    }

    private async Task UrediAsync()
    {
        Ui.Naslov("Uredivanje zapisa");

        var zapis = await OdaberiZapisAsync();
        if (zapis is null) return;

        zapis.DatumOd = Ui.Datum("Datum pocetka:", zapis.DatumOd);
        zapis.DatumDo = Ui.DatumOpcionalno("Datum zavrsetka:", zapis.DatumDo);
        zapis.Napomena = Ui.TekstOpcionalno("Napomena:", zapis.Napomena);

        var (uspjeh, poruka) = await _povijest.SpremiPromjeneAsync(zapis);
        if (uspjeh) Ui.Uspjeh(poruka); else Ui.Greska(poruka);
        Ui.Pauza();
    }

    private async Task ObrisiAsync()
    {
        Ui.Naslov("Brisanje zapisa");

        var zapis = await OdaberiZapisAsync();
        if (zapis is null) return;

        if (!Ui.Potvrdi("Sigurno obrisati zapis?")) return;

        var (uspjeh, poruka) = await _povijest.ObrisiAsync(zapis.Id);
        if (uspjeh) Ui.Uspjeh(poruka); else Ui.Greska(poruka);
        Ui.Pauza();
    }

    private async Task<PovijestBolesti?> OdaberiZapisAsync()
    {
        var pacijent = await _odabir.PacijentAsync();
        if (pacijent is null) return null;

        var lista = await _povijest.DohvatiZaPacijentaAsync(pacijent.Id);
        if (lista.Count == 0)
        {
            Ui.Info("Pacijent nema zapisa povijesti bolesti.");
            Ui.Pauza();
            return null;
        }

        var odabrani = Ui.Odaberi("Odaberi zapis:", lista,
            pb => $"{pb.Dijagnoza?.Sifra} {pb.Dijagnoza?.Naziv} ({pb.DatumOd:dd.MM.yyyy})");

        return await _povijest.DohvatiAsync(odabrani.Id);
    }
}
