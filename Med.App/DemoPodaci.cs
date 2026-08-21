using Med.Data.Ef;
using Med.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Med.App;

public static class DemoPodaci
{
    // Npgsql odbija DateTime s Kind=Local za "timestamp without time zone".
    private static DateTime D(int godina, int mjesec, int dan) =>
        new(godina, mjesec, dan, 0, 0, 0, DateTimeKind.Unspecified);

    // ...i prima samo offset 0 za "timestamp with time zone", pa lokalno
    // zidno vrijeme termina pretvaramo u UTC.
    private static DateTimeOffset T(DateTime lokalno) =>
        new DateTimeOffset(DateTime.SpecifyKind(lokalno, DateTimeKind.Local)).ToUniversalTime();

    public static async Task<string> UcitajAsync(MedDbContext db)
    {
        if (await db.Pacijenti.AnyAsync())
            return "Pacijenti vec postoje - demo podaci nisu ucitani.";

        var lijecnici = await db.Lijecnici.OrderBy(l => l.Id).ToListAsync();
        var dijagnoze = await db.Dijagnoze.OrderBy(d => d.Id).ToListAsync();
        var lijekovi = await db.Lijekovi.OrderBy(l => l.Id).ToListAsync();

        if (lijecnici.Count < 5 || dijagnoze.Count < 5 || lijekovi.Count < 5)
            return "Sifrarnici nisu popunjeni - demo podaci nisu ucitani.";

        var adrese = new List<Adresa>
        {
            new() { Ulica = "Ilica", KucniBroj = "112", Grad = "Zagreb", PostanskiBroj = "10000" },
            new() { Ulica = "Vukovarska", KucniBroj = "8", Grad = "Zagreb", PostanskiBroj = "10000" },
            new() { Ulica = "Splitska", KucniBroj = "45a", Grad = "Split", PostanskiBroj = "21000" },
            new() { Ulica = "Korzo", KucniBroj = "3", Grad = "Rijeka", PostanskiBroj = "51000" }
        };
        db.Adrese.AddRange(adrese);
        await db.SaveChangesAsync();

        var pacijenti = new List<Pacijent>
        {
            new()
            {
                Ime = "Ivan", Prezime = "Maric", Oib = "12345678901",
                DatumRodenja = D(1985, 3, 12), Spol = 'M',
                AdresaBoravistaId = adrese[0].Id, AdresaPrebivalistaId = adrese[1].Id,
                KreiranoNa = DateTimeOffset.UtcNow
            },
            new()
            {
                Ime = "Petra", Prezime = "Kovacevic", Oib = "23456789012",
                DatumRodenja = D(1992, 11, 4), Spol = 'Z',
                AdresaBoravistaId = adrese[2].Id,
                KreiranoNa = DateTimeOffset.UtcNow
            },
            new()
            {
                Ime = "Josip", Prezime = "Novak", Oib = "34567890123",
                DatumRodenja = D(1958, 6, 27), Spol = 'M',
                AdresaBoravistaId = adrese[3].Id,
                KreiranoNa = DateTimeOffset.UtcNow
            }
        };
        db.Pacijenti.AddRange(pacijenti);
        await db.SaveChangesAsync();

        db.Kartoni.AddRange(
            new KartonPacijenta
            {
                PacijentId = pacijenti[0].Id, KrvnaGrupa = "A+",
                VisinaCm = 182, TezinaKg = 88.5, Alergije = "Penicilin"
            },
            new KartonPacijenta
            {
                PacijentId = pacijenti[1].Id, KrvnaGrupa = "0-",
                VisinaCm = 168, TezinaKg = 61
            },
            new KartonPacijenta
            {
                PacijentId = pacijenti[2].Id, KrvnaGrupa = "B+",
                VisinaCm = 175, TezinaKg = 94.2, Alergije = "Peludna groznica, jod"
            });
        await db.SaveChangesAsync();

        var povijest = new List<PovijestBolesti>
        {
            new()
            {
                PacijentId = pacijenti[0].Id, DijagnozaId = dijagnoze[0].Id, LijecnikId = lijecnici[1].Id,
                DatumOd = D(2021, 5, 10), Napomena = "Redovita kontrola tlaka svaka tri mjeseca."
            },
            new()
            {
                PacijentId = pacijenti[0].Id, DijagnozaId = dijagnoze[4].Id, LijecnikId = lijecnici[0].Id,
                DatumOd = D(2024, 1, 8), DatumDo = D(2024, 2, 20), Napomena = "Bolovanje 6 tjedana."
            },
            new()
            {
                PacijentId = pacijenti[1].Id, DijagnozaId = dijagnoze[2].Id, LijecnikId = lijecnici[0].Id,
                DatumOd = D(2019, 9, 1), Napomena = "Sezonsko pogorsanje u proljece."
            },
            new()
            {
                PacijentId = pacijenti[2].Id, DijagnozaId = dijagnoze[1].Id, LijecnikId = lijecnici[1].Id,
                DatumOd = D(2016, 2, 14)
            },
            new()
            {
                PacijentId = pacijenti[2].Id, DijagnozaId = dijagnoze[3].Id, LijecnikId = lijecnici[0].Id,
                DatumOd = D(2023, 7, 3), DatumDo = D(2023, 10, 1)
            }
        };
        db.PovijestiBolesti.AddRange(povijest);
        await db.SaveChangesAsync();

        db.Terapije.AddRange(
            new Terapija
            {
                PacijentId = pacijenti[0].Id, LijekId = lijekovi[0].Id, LijecnikId = lijecnici[1].Id,
                PovijestBolestiId = povijest[0].Id, Doza = 5, JedinicaDoze = "mg",
                Ucestalost = "jednom dnevno ujutro", DatumOd = D(2021, 5, 10)
            },
            new Terapija
            {
                PacijentId = pacijenti[0].Id, LijekId = lijekovi[4].Id, LijecnikId = lijecnici[0].Id,
                PovijestBolestiId = povijest[1].Id, Doza = 400, JedinicaDoze = "mg",
                Ucestalost = "3 puta dnevno", DatumOd = D(2024, 1, 8),
                DatumDo = D(2024, 2, 20), Aktivna = false
            },
            new Terapija
            {
                PacijentId = pacijenti[1].Id, LijekId = lijekovi[2].Id, LijecnikId = lijecnici[0].Id,
                PovijestBolestiId = povijest[2].Id, Doza = 2, JedinicaDoze = "udaha",
                Ucestalost = "po potrebi, najvise 4 puta dnevno", DatumOd = D(2019, 9, 1)
            },
            new Terapija
            {
                PacijentId = pacijenti[2].Id, LijekId = lijekovi[1].Id, LijecnikId = lijecnici[1].Id,
                PovijestBolestiId = povijest[3].Id, Doza = 850, JedinicaDoze = "mg",
                Ucestalost = "2 puta dnevno uz obrok", DatumOd = D(2016, 2, 14)
            },
            new Terapija
            {
                PacijentId = pacijenti[2].Id, LijekId = lijekovi[3].Id, LijecnikId = lijecnici[0].Id,
                PovijestBolestiId = povijest[4].Id, Doza = 40, JedinicaDoze = "mg",
                Ucestalost = "jednom dnevno nataste", DatumOd = D(2023, 7, 3),
                DatumDo = D(2023, 10, 1), Aktivna = false
            });
        await db.SaveChangesAsync();

        var danas = DateTime.Today;
        db.Pregledi.AddRange(
            new Pregled
            {
                PacijentId = pacijenti[0].Id, LijecnikId = lijecnici[2].Id, UputiteljId = lijecnici[1].Id,
                Tip = TipPregleda.EKG, Termin = T(danas.AddDays(7).AddHours(9)),
                TrajanjeMinuta = 30, Status = StatusPregleda.Zakazan
            },
            new Pregled
            {
                PacijentId = pacijenti[0].Id, LijecnikId = lijecnici[2].Id, UputiteljId = lijecnici[0].Id,
                Tip = TipPregleda.CT, Termin = T(danas.AddMonths(-4).AddHours(11)),
                TrajanjeMinuta = 45, Status = StatusPregleda.Odrzan,
                Nalaz = "Uredan nalaz, bez znakova patoloskih promjena."
            },
            new Pregled
            {
                PacijentId = pacijenti[1].Id, LijecnikId = lijecnici[4].Id, UputiteljId = lijecnici[0].Id,
                Tip = TipPregleda.DERM, Termin = T(danas.AddDays(3).AddHours(14)),
                TrajanjeMinuta = 20, Status = StatusPregleda.Zakazan
            },
            new Pregled
            {
                PacijentId = pacijenti[2].Id, LijecnikId = lijecnici[3].Id,
                Tip = TipPregleda.EEG, Termin = T(danas.AddDays(14).AddHours(8).AddMinutes(30)),
                TrajanjeMinuta = 60, Status = StatusPregleda.Zakazan
            },
            new Pregled
            {
                PacijentId = pacijenti[2].Id, LijecnikId = lijecnici[5].Id, UputiteljId = lijecnici[1].Id,
                Tip = TipPregleda.OKO, Termin = T(danas.AddMonths(-1).AddHours(16)),
                TrajanjeMinuta = 25, Status = StatusPregleda.Otkazan
            });
        await db.SaveChangesAsync();

        return $"Ucitano: {pacijenti.Count} pacijenta, {povijest.Count} zapisa povijesti, 5 terapija, 5 pregleda.";
    }
}
