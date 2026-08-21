using Med.App.Servisi;
using Med.Domain.Entities;
using Spectre.Console;

namespace Med.App.Izbornici;

public class TerapijaIzbornik
{
    private readonly TerapijaService _terapije;
    private readonly PovijestBolestiService _povijest;
    private readonly Odabir _odabir;

    public TerapijaIzbornik(TerapijaService terapije, PovijestBolestiService povijest, Odabir odabir)
    {
        _terapije = terapije;
        _povijest = povijest;
        _odabir = odabir;
    }

    public async Task PokreniAsync()
    {
        while (true)
        {
            Ui.Naslov("Terapije");

            var izbor = Ui.Odaberi("Odaberi radnju:", new[]
            {
                "Prikaz za pacijenta",
                "Nova terapija",
                "Uredi terapiju",
                "Zavrsi terapiju",
                "Obrisi terapiju",
                "Natrag"
            }, x => x);

            switch (izbor)
            {
                case "Prikaz za pacijenta": await PrikazAsync(); break;
                case "Nova terapija": await NovaAsync(); break;
                case "Uredi terapiju": await UrediAsync(); break;
                case "Zavrsi terapiju": await ZavrsiAsync(); break;
                case "Obrisi terapiju": await ObrisiAsync(); break;
                default: return;
            }
        }
    }

    private async Task PrikazAsync()
    {
        Ui.Naslov("Terapije pacijenta");

        var pacijent = await _odabir.PacijentAsync();
        if (pacijent is null) return;

        Ispisi(await _terapije.DohvatiZaPacijentaAsync(pacijent.Id));
        Ui.Pauza();
    }

    private static void Ispisi(List<Terapija> lista)
    {
        if (lista.Count == 0)
        {
            Ui.Info("Nema terapija.");
            return;
        }

        var tablica = new Table().Border(TableBorder.Rounded);
        tablica.AddColumn("Id");
        tablica.AddColumn("Lijek");
        tablica.AddColumn("Doza");
        tablica.AddColumn("Ucestalost");
        tablica.AddColumn("Od");
        tablica.AddColumn("Do");
        tablica.AddColumn("Aktivna");
        tablica.AddColumn("Za dijagnozu");

        foreach (var t in lista)
            tablica.AddRow(
                t.Id.ToString(),
                t.Lijek?.Naziv ?? "-",
                $"{t.Doza:0.##} {t.JedinicaDoze}",
                t.Ucestalost,
                t.DatumOd.ToString("dd.MM.yyyy"),
                t.DatumDo?.ToString("dd.MM.yyyy") ?? "-",
                t.Aktivna ? "da" : "ne",
                t.PovijestBolesti?.Dijagnoza?.Sifra ?? "-");

        AnsiConsole.Write(tablica);
    }

    private async Task NovaAsync()
    {
        Ui.Naslov("Nova terapija");

        var pacijent = await _odabir.PacijentAsync();
        if (pacijent is null) return;

        var lijek = await _odabir.LijekAsync();
        if (lijek is null) return;

        var lijecnik = await _odabir.LijecnikAsync("Lijecnik koji propisuje:");
        if (lijecnik is null) return;

        int? povijestId = null;
        var zapisi = await _povijest.DohvatiZaPacijentaAsync(pacijent.Id);
        if (zapisi.Count > 0 && Ui.Potvrdi("Povezati terapiju s dijagnozom iz povijesti bolesti?"))
        {
            var zapis = Ui.Odaberi("Odaberi dijagnozu:", zapisi,
                pb => $"{pb.Dijagnoza?.Sifra} {pb.Dijagnoza?.Naziv} ({pb.DatumOd:dd.MM.yyyy})");
            povijestId = zapis.Id;
        }

        var terapija = new Terapija
        {
            PacijentId = pacijent.Id,
            LijekId = lijek.Id,
            LijecnikId = lijecnik.Id,
            PovijestBolestiId = povijestId,
            Doza = Ui.Decimalni("Doza:"),
            JedinicaDoze = Ui.Tekst("Jedinica doze [grey](mg, tableta, ml...)[/]:"),
            Ucestalost = Ui.Tekst("Ucestalost [grey](npr. 3 puta dnevno)[/]:"),
            DatumOd = Ui.Datum("Datum pocetka:"),
            DatumDo = Ui.DatumOpcionalno("Datum zavrsetka:")
        };

        await _terapije.DodajAsync(terapija);
        Ui.Uspjeh("Terapija spremljena.");
        Ui.Pauza();
    }

    private async Task UrediAsync()
    {
        Ui.Naslov("Uredivanje terapije");

        var terapija = await OdaberiAsync();
        if (terapija is null) return;

        terapija.Doza = Ui.Decimalni("Doza:", terapija.Doza);
        terapija.JedinicaDoze = Ui.Tekst("Jedinica doze:", terapija.JedinicaDoze);
        terapija.Ucestalost = Ui.Tekst("Ucestalost:", terapija.Ucestalost);
        terapija.DatumDo = Ui.DatumOpcionalno("Datum zavrsetka:", terapija.DatumDo);

        await _terapije.SpremiPromjeneAsync();
        Ui.Uspjeh("Terapija azurirana.");
        Ui.Pauza();
    }

    private async Task ZavrsiAsync()
    {
        Ui.Naslov("Zavrsetak terapije");

        var terapija = await OdaberiAsync();
        if (terapija is null) return;

        await _terapije.ZavrsiAsync(terapija.Id);
        Ui.Uspjeh("Terapija oznacena kao zavrsena.");
        Ui.Pauza();
    }

    private async Task ObrisiAsync()
    {
        Ui.Naslov("Brisanje terapije");

        var terapija = await OdaberiAsync();
        if (terapija is null) return;

        if (!Ui.Potvrdi("Sigurno obrisati terapiju?")) return;

        await _terapije.ObrisiAsync(terapija.Id);
        Ui.Uspjeh("Terapija obrisana.");
        Ui.Pauza();
    }

    private async Task<Terapija?> OdaberiAsync()
    {
        var pacijent = await _odabir.PacijentAsync();
        if (pacijent is null) return null;

        var lista = await _terapije.DohvatiZaPacijentaAsync(pacijent.Id);
        if (lista.Count == 0)
        {
            Ui.Info("Pacijent nema terapija.");
            Ui.Pauza();
            return null;
        }

        var odabrana = Ui.Odaberi("Odaberi terapiju:", lista,
            t => $"{t.Lijek?.Naziv} {t.Doza:0.##} {t.JedinicaDoze} ({t.Ucestalost})");

        return await _terapije.DohvatiAsync(odabrana.Id);
    }
}
