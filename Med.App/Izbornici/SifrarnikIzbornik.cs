using Med.App.Servisi;
using Med.Domain.Entities;
using Spectre.Console;

namespace Med.App.Izbornici;

public class SifrarnikIzbornik
{
    private readonly SifrarnikService _sifrarnik;

    public SifrarnikIzbornik(SifrarnikService sifrarnik) => _sifrarnik = sifrarnik;

    public async Task PokreniAsync()
    {
        while (true)
        {
            Ui.Naslov("Sifrarnici");

            var izbor = Ui.Odaberi("Odaberi sifrarnik:", new[]
            {
                "Dijagnoze",
                "Lijekovi",
                "Adrese",
                "Lijecnici (samo pregled)",
                "Natrag"
            }, x => x);

            switch (izbor)
            {
                case "Dijagnoze": await DijagnozeAsync(); break;
                case "Lijekovi": await LijekoviAsync(); break;
                case "Adrese": await AdreseAsync(); break;
                case "Lijecnici (samo pregled)": await LijecniciAsync(); break;
                default: return;
            }
        }
    }

    private static string? Radnja(string naslov)
    {
        var izbor = Ui.Odaberi(naslov, new[] { "Popis", "Novi", "Uredi", "Obrisi", "Natrag" }, x => x);
        return izbor == "Natrag" ? null : izbor;
    }

    // ---------- dijagnoze ----------

    private async Task DijagnozeAsync()
    {
        while (true)
        {
            Ui.Naslov("Dijagnoze");

            var radnja = Radnja("Odaberi radnju:");
            if (radnja is null) return;

            switch (radnja)
            {
                case "Popis":
                    Ui.Naslov("Dijagnoze");
                    IspisiDijagnoze(await _sifrarnik.DijagnozeAsync());
                    Ui.Pauza();
                    break;

                case "Novi":
                    Ui.Naslov("Nova dijagnoza");
                    var nova = new Dijagnoza
                    {
                        Sifra = Ui.Tekst("Sifra [grey](MKB-10, npr. I10)[/]:"),
                        Naziv = Ui.Tekst("Naziv:"),
                        Opis = Ui.TekstOpcionalno("Opis:")
                    };
                    Javi(await _sifrarnik.DodajDijagnozuAsync(nova));
                    break;

                case "Uredi":
                    Ui.Naslov("Uredivanje dijagnoze");
                    var zaUredit = await OdaberiDijagnozuAsync();
                    if (zaUredit is null) break;

                    zaUredit.Sifra = Ui.Tekst("Sifra:", zaUredit.Sifra);
                    zaUredit.Naziv = Ui.Tekst("Naziv:", zaUredit.Naziv);
                    zaUredit.Opis = Ui.TekstOpcionalno("Opis:", zaUredit.Opis);
                    Javi(await _sifrarnik.AzurirajDijagnozuAsync(zaUredit));
                    break;

                case "Obrisi":
                    Ui.Naslov("Brisanje dijagnoze");
                    var zaObrisat = await OdaberiDijagnozuAsync();
                    if (zaObrisat is null) break;

                    if (!Ui.Potvrdi($"Sigurno obrisati {zaObrisat.Sifra} - {zaObrisat.Naziv}?")) break;
                    Javi(await _sifrarnik.ObrisiDijagnozuAsync(zaObrisat.Id));
                    break;
            }
        }
    }

    private static void IspisiDijagnoze(List<Dijagnoza> lista)
    {
        if (lista.Count == 0) { Ui.Info("Nema dijagnoza."); return; }

        var tablica = new Table().Border(TableBorder.Rounded);
        tablica.AddColumn("Id");
        tablica.AddColumn("Sifra");
        tablica.AddColumn("Naziv");
        tablica.AddColumn("Opis");

        foreach (var d in lista)
            tablica.AddRow(d.Id.ToString(), d.Sifra, d.Naziv, d.Opis ?? "-");

        AnsiConsole.Write(tablica);
    }

    private async Task<Dijagnoza?> OdaberiDijagnozuAsync()
    {
        var lista = await _sifrarnik.DijagnozeAsync();
        if (lista.Count == 0)
        {
            Ui.Info("Nema dijagnoza.");
            Ui.Pauza();
            return null;
        }

        var odabrana = Ui.Odaberi("Odaberi dijagnozu:", lista, d => d.ToString());
        return await _sifrarnik.DijagnozaAsync(odabrana.Id);
    }

    // ---------- lijekovi ----------

    private async Task LijekoviAsync()
    {
        while (true)
        {
            Ui.Naslov("Lijekovi");

            var radnja = Radnja("Odaberi radnju:");
            if (radnja is null) return;

            switch (radnja)
            {
                case "Popis":
                    Ui.Naslov("Lijekovi");
                    IspisiLijekove(await _sifrarnik.LijekoviAsync());
                    Ui.Pauza();
                    break;

                case "Novi":
                    Ui.Naslov("Novi lijek");
                    var novi = new Lijek
                    {
                        Naziv = Ui.Tekst("Naziv:"),
                        AtcKod = Ui.TekstOpcionalno("ATC kod:"),
                        Oblik = Ui.Tekst("Oblik [grey](tableta, kapsula, inhalator...)[/]:")
                    };
                    Javi(await _sifrarnik.DodajLijekAsync(novi));
                    break;

                case "Uredi":
                    Ui.Naslov("Uredivanje lijeka");
                    var zaUredit = await OdaberiLijekAsync();
                    if (zaUredit is null) break;

                    zaUredit.Naziv = Ui.Tekst("Naziv:", zaUredit.Naziv);
                    zaUredit.AtcKod = Ui.TekstOpcionalno("ATC kod:", zaUredit.AtcKod);
                    zaUredit.Oblik = Ui.Tekst("Oblik:", zaUredit.Oblik);
                    Javi(await _sifrarnik.AzurirajLijekAsync(zaUredit));
                    break;

                case "Obrisi":
                    Ui.Naslov("Brisanje lijeka");
                    var zaObrisat = await OdaberiLijekAsync();
                    if (zaObrisat is null) break;

                    if (!Ui.Potvrdi($"Sigurno obrisati {zaObrisat.Naziv}?")) break;
                    Javi(await _sifrarnik.ObrisiLijekAsync(zaObrisat.Id));
                    break;
            }
        }
    }

    private static void IspisiLijekove(List<Lijek> lista)
    {
        if (lista.Count == 0) { Ui.Info("Nema lijekova."); return; }

        var tablica = new Table().Border(TableBorder.Rounded);
        tablica.AddColumn("Id");
        tablica.AddColumn("Naziv");
        tablica.AddColumn("ATC kod");
        tablica.AddColumn("Oblik");

        foreach (var l in lista)
            tablica.AddRow(l.Id.ToString(), l.Naziv, l.AtcKod ?? "-", l.Oblik);

        AnsiConsole.Write(tablica);
    }

    private async Task<Lijek?> OdaberiLijekAsync()
    {
        var lista = await _sifrarnik.LijekoviAsync();
        if (lista.Count == 0)
        {
            Ui.Info("Nema lijekova.");
            Ui.Pauza();
            return null;
        }

        var odabrani = Ui.Odaberi("Odaberi lijek:", lista, l => l.ToString());
        return await _sifrarnik.LijekAsync(odabrani.Id);
    }

    // ---------- adrese ----------

    private async Task AdreseAsync()
    {
        while (true)
        {
            Ui.Naslov("Adrese");

            var radnja = Radnja("Odaberi radnju:");
            if (radnja is null) return;

            switch (radnja)
            {
                case "Popis":
                    Ui.Naslov("Adrese");
                    await IspisiAdreseAsync();
                    Ui.Pauza();
                    break;

                case "Novi":
                    Ui.Naslov("Nova adresa");
                    Javi(await _sifrarnik.DodajAdresuAsync(UnesiAdresu()));
                    break;

                case "Uredi":
                    Ui.Naslov("Uredivanje adrese");
                    var zaUredit = await OdaberiAdresuAsync();
                    if (zaUredit is null) break;

                    var korisnika = await _sifrarnik.BrojKorisnikaAdreseAsync(zaUredit.Id);
                    if (korisnika > 0)
                        Ui.Info($"Adresu koristi {korisnika} pacijenata - izmjena vrijedi za sve.");

                    zaUredit.Ulica = Ui.Tekst("Ulica:", zaUredit.Ulica);
                    zaUredit.KucniBroj = Ui.TekstOpcionalno("Kucni broj:", zaUredit.KucniBroj);
                    zaUredit.Grad = Ui.Tekst("Grad:", zaUredit.Grad);
                    zaUredit.PostanskiBroj = Ui.TekstOpcionalno("Postanski broj:", zaUredit.PostanskiBroj);
                    zaUredit.Drzava = Ui.Tekst("Drzava:", zaUredit.Drzava);
                    Javi(await _sifrarnik.AzurirajAdresuAsync(zaUredit));
                    break;

                case "Obrisi":
                    Ui.Naslov("Brisanje adrese");
                    var zaObrisat = await OdaberiAdresuAsync();
                    if (zaObrisat is null) break;

                    if (!Ui.Potvrdi($"Sigurno obrisati {zaObrisat}?")) break;
                    Javi(await _sifrarnik.ObrisiAdresuAsync(zaObrisat.Id));
                    break;
            }
        }
    }

    private static Adresa UnesiAdresu() => new()
    {
        Ulica = Ui.Tekst("Ulica:"),
        KucniBroj = Ui.TekstOpcionalno("Kucni broj:"),
        Grad = Ui.Tekst("Grad:"),
        PostanskiBroj = Ui.TekstOpcionalno("Postanski broj:"),
        Drzava = Ui.Tekst("Drzava:", "Hrvatska")
    };

    private async Task IspisiAdreseAsync()
    {
        var lista = await _sifrarnik.AdreseAsync();
        if (lista.Count == 0) { Ui.Info("Nema adresa."); return; }

        var brojevi = await _sifrarnik.BrojeviKorisnikaAdresaAsync();

        var tablica = new Table().Border(TableBorder.Rounded);
        tablica.AddColumn("Id");
        tablica.AddColumn("Adresa");
        tablica.AddColumn("Drzava");
        tablica.AddColumn("Koriste");

        foreach (var a in lista)
            tablica.AddRow(
                a.Id.ToString(),
                a.ToString(),
                a.Drzava,
                brojevi.GetValueOrDefault(a.Id).ToString());

        AnsiConsole.Write(tablica);
    }

    private async Task<Adresa?> OdaberiAdresuAsync()
    {
        var lista = await _sifrarnik.AdreseAsync();
        if (lista.Count == 0)
        {
            Ui.Info("Nema adresa.");
            Ui.Pauza();
            return null;
        }

        var odabrana = Ui.Odaberi("Odaberi adresu:", lista, a => a.ToString());
        return await _sifrarnik.AdresaAsync(odabrana.Id);
    }

    // ---------- lijecnici ----------

    private async Task LijecniciAsync()
    {
        Ui.Naslov("Lijecnici");
        Ui.Info("Lijecnici se ne ureduju kroz CRUD - unose se pri prvom pokretanju aplikacije.\n");

        var tablica = new Table().Border(TableBorder.Rounded);
        tablica.AddColumn("Id");
        tablica.AddColumn("Prezime, ime");
        tablica.AddColumn("Specijalizacija");

        foreach (var l in await _sifrarnik.LijecniciAsync())
            tablica.AddRow(l.Id.ToString(), $"{l.Prezime}, {l.Ime}", l.Specijalizacija);

        AnsiConsole.Write(tablica);
        Ui.Pauza();
    }

    private static void Javi((bool uspjeh, string poruka) ishod)
    {
        if (ishod.uspjeh) Ui.Uspjeh(ishod.poruka); else Ui.Greska(ishod.poruka);
        Ui.Pauza();
    }
}
