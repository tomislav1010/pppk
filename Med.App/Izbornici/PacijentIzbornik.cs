using Med.App.Servisi;
using Med.Domain.Entities;
using Spectre.Console;

namespace Med.App.Izbornici;

public class PacijentIzbornik
{
    private readonly PacijentService _pacijenti;

    public PacijentIzbornik(PacijentService pacijenti) => _pacijenti = pacijenti;

    public async Task PokreniAsync()
    {
        while (true)
        {
            Ui.Naslov("Pacijenti");

            var izbor = Ui.Odaberi("Odaberi radnju:", new[]
            {
                "Popis svih pacijenata",
                "Pretrazi",
                "Novi pacijent",
                "Uredi pacijenta",
                "Karton pacijenta",
                "Obrisi pacijenta",
                "Natrag"
            }, x => x);

            switch (izbor)
            {
                case "Popis svih pacijenata": await PopisAsync(); break;
                case "Pretrazi": await PretragaAsync(); break;
                case "Novi pacijent": await NoviAsync(); break;
                case "Uredi pacijenta": await UrediAsync(); break;
                case "Karton pacijenta": await KartonAsync(); break;
                case "Obrisi pacijenta": await ObrisiAsync(); break;
                default: return;
            }
        }
    }

    private async Task PopisAsync()
    {
        Ui.Naslov("Popis pacijenata");
        Ispisi(await _pacijenti.DohvatiSveAsync());
        Ui.Pauza();
    }

    private async Task PretragaAsync()
    {
        Ui.Naslov("Pretraga pacijenata");
        var pojam = Ui.Tekst("Ime, prezime ili OIB:");
        Ispisi(await _pacijenti.PretraziAsync(pojam));
        Ui.Pauza();
    }

    private static void Ispisi(List<Pacijent> lista)
    {
        if (lista.Count == 0)
        {
            Ui.Info("Nema pacijenata.");
            return;
        }

        var tablica = new Table().Border(TableBorder.Rounded);
        tablica.AddColumn("Id");
        tablica.AddColumn("Prezime, ime");
        tablica.AddColumn("OIB");
        tablica.AddColumn("Rodenje");
        tablica.AddColumn("Spol");
        tablica.AddColumn("Boraviste");

        foreach (var p in lista)
            tablica.AddRow(
                p.Id.ToString(),
                $"{p.Prezime}, {p.Ime}",
                p.Oib,
                p.DatumRodenja.ToString("dd.MM.yyyy"),
                p.Spol.ToString(),
                p.AdresaBoravista?.ToString() ?? "-");

        AnsiConsole.Write(tablica);
    }

    private async Task NoviAsync()
    {
        Ui.Naslov("Novi pacijent");

        var pacijent = new Pacijent
        {
            Ime = Ui.Tekst("Ime:"),
            Prezime = Ui.Tekst("Prezime:"),
            Oib = Ui.Tekst("OIB:"),
            DatumRodenja = Ui.Datum("Datum rodenja:"),
            Spol = Ui.Odaberi("Spol:", new[] { 'M', 'Z' }, x => x.ToString()),
            Telefon = Ui.TekstOpcionalno("Telefon:")
        };

        var greska = PacijentService.ValidirajOib(pacijent.Oib);
        if (greska is not null)
        {
            Ui.Greska(greska);
            Ui.Pauza();
            return;
        }

        AnsiConsole.WriteLine();
        Ui.Info("Adresa boravista:");
        var boraviste = UnesiAdresu();

        Adresa? prebivaliste = null;
        AnsiConsole.WriteLine();
        if (Ui.Potvrdi("Prebivaliste se razlikuje od boravista?"))
        {
            Ui.Info("Adresa prebivalista:");
            prebivaliste = UnesiAdresu();
        }

        var (uspjeh, poruka) = await _pacijenti.DodajAsync(pacijent, boraviste, prebivaliste);
        if (uspjeh) Ui.Uspjeh(poruka); else Ui.Greska(poruka);
        Ui.Pauza();
    }

    private static Adresa UnesiAdresu() => new()
    {
        Ulica = Ui.Tekst("  Ulica:"),
        KucniBroj = Ui.TekstOpcionalno("  Kucni broj:"),
        Grad = Ui.Tekst("  Grad:"),
        PostanskiBroj = Ui.TekstOpcionalno("  Postanski broj:"),
        Drzava = Ui.Tekst("  Drzava:", "Hrvatska")
    };

    private async Task UrediAsync()
    {
        Ui.Naslov("Uredivanje pacijenta");

        var pacijent = await OdaberiPacijentaAsync();
        if (pacijent is null) return;

        pacijent.Ime = Ui.Tekst("Ime:", pacijent.Ime);
        pacijent.Prezime = Ui.Tekst("Prezime:", pacijent.Prezime);
        pacijent.Oib = Ui.Tekst("OIB:", pacijent.Oib);
        pacijent.DatumRodenja = Ui.Datum("Datum rodenja:", pacijent.DatumRodenja);
        pacijent.Spol = Ui.Odaberi("Spol:", new[] { 'M', 'Z' }, x => x.ToString());
        pacijent.Telefon = Ui.TekstOpcionalno("Telefon:", pacijent.Telefon);

        var (uspjeh, poruka) = await _pacijenti.AzurirajAsync(pacijent);
        if (uspjeh) Ui.Uspjeh(poruka); else Ui.Greska(poruka);
        Ui.Pauza();
    }

    private async Task KartonAsync()
    {
        Ui.Naslov("Karton pacijenta");

        var pacijent = await OdaberiPacijentaAsync();
        if (pacijent is null) return;

        var puni = await _pacijenti.DohvatiPunAsync(pacijent.Id);
        var postojeci = puni?.Karton;

        if (postojeci is not null)
            Ui.Info($"Postojeci karton: {postojeci.KrvnaGrupa}, {postojeci.VisinaCm} cm, {postojeci.TezinaKg} kg");

        var karton = new KartonPacijenta
        {
            KrvnaGrupa = Ui.TekstOpcionalno("Krvna grupa:", postojeci?.KrvnaGrupa),
            VisinaCm = Ui.DecimalniOpcionalno("Visina (cm):", postojeci?.VisinaCm),
            TezinaKg = Ui.DecimalniOpcionalno("Tezina (kg):", postojeci?.TezinaKg),
            Alergije = Ui.TekstOpcionalno("Alergije:", postojeci?.Alergije)
        };

        await _pacijenti.PostaviKartonAsync(pacijent.Id, karton);
        Ui.Uspjeh("Karton spremljen.");
        Ui.Pauza();
    }

    private async Task ObrisiAsync()
    {
        Ui.Naslov("Brisanje pacijenta");

        var pacijent = await OdaberiPacijentaAsync();
        if (pacijent is null) return;

        Ui.Info("Brisanjem se kaskadno brisu karton, povijest bolesti, terapije i pregledi.");
        if (!Ui.Potvrdi($"Sigurno obrisati pacijenta {pacijent.Prezime}, {pacijent.Ime}?"))
            return;

        var (uspjeh, poruka) = await _pacijenti.ObrisiAsync(pacijent.Id);
        if (uspjeh) Ui.Uspjeh(poruka); else Ui.Greska(poruka);
        Ui.Pauza();
    }

    public async Task<Pacijent?> OdaberiPacijentaAsync()
    {
        var lista = await _pacijenti.DohvatiSveAsync();
        if (lista.Count == 0)
        {
            Ui.Info("Nema pacijenata u bazi.");
            Ui.Pauza();
            return null;
        }

        var odabrani = Ui.Odaberi("Odaberi pacijenta:", lista, p => $"{p.Prezime}, {p.Ime} ({p.Oib})");

        // popis dolazi iz AsNoTracking upita, pa za uredivanje trebamo pracenu instancu
        return await _pacijenti.DohvatiAsync(odabrani.Id);
    }
}
