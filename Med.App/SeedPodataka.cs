using Med.Data.Ef;
using Med.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Med.App;

public static class SeedPodataka
{
    public static async Task PokreniAsync(MedDbContext db)
    {
        await SeedLijecnikaAsync(db);
        await SeedDijagnozaAsync(db);
        await SeedLijekovaAsync(db);
    }

    private static async Task SeedLijecnikaAsync(MedDbContext db)
    {
        if (await db.Lijecnici.AnyAsync()) return;

        db.Lijecnici.AddRange(
            new Lijecnik { Ime = "Ana", Prezime = "Horvat", Specijalizacija = "Obiteljska medicina" },
            new Lijecnik { Ime = "Marko", Prezime = "Kovac", Specijalizacija = "Kardiologija" },
            new Lijecnik { Ime = "Ivana", Prezime = "Novak", Specijalizacija = "Radiologija" },
            new Lijecnik { Ime = "Petar", Prezime = "Babic", Specijalizacija = "Neurologija" },
            new Lijecnik { Ime = "Lucija", Prezime = "Maric", Specijalizacija = "Dermatologija" },
            new Lijecnik { Ime = "Tomislav", Prezime = "Juric", Specijalizacija = "Oftalmologija" },
            new Lijecnik { Ime = "Marija", Prezime = "Vukovic", Specijalizacija = "Stomatologija" }
        );

        await db.SaveChangesAsync();
        Console.WriteLine("Uneseni lijecnici (prvo pokretanje).");
    }

    private static async Task SeedDijagnozaAsync(MedDbContext db)
    {
        if (await db.Dijagnoze.AnyAsync()) return;

        db.Dijagnoze.AddRange(
            new Dijagnoza { Sifra = "I10", Naziv = "Esencijalna hipertenzija", Opis = "Povisen krvni tlak bez poznatog uzroka." },
            new Dijagnoza { Sifra = "E11", Naziv = "Secerna bolest tipa 2" },
            new Dijagnoza { Sifra = "J45", Naziv = "Astma" },
            new Dijagnoza { Sifra = "K21", Naziv = "Gastroezofagealna refluksna bolest" },
            new Dijagnoza { Sifra = "M54", Naziv = "Dorzalgija", Opis = "Bol u ledima." },
            new Dijagnoza { Sifra = "F41", Naziv = "Anksiozni poremecaj" },
            new Dijagnoza { Sifra = "J06", Naziv = "Akutna infekcija gornjih disnih putova" },
            new Dijagnoza { Sifra = "L20", Naziv = "Atopijski dermatitis" }
        );

        await db.SaveChangesAsync();
        Console.WriteLine("Unesene dijagnoze (prvo pokretanje).");
    }

    private static async Task SeedLijekovaAsync(MedDbContext db)
    {
        if (await db.Lijekovi.AnyAsync()) return;

        db.Lijekovi.AddRange(
            new Lijek { Naziv = "Ramipril", AtcKod = "C09AA05", Oblik = "tableta" },
            new Lijek { Naziv = "Metformin", AtcKod = "A10BA02", Oblik = "tableta" },
            new Lijek { Naziv = "Salbutamol", AtcKod = "R03AC02", Oblik = "inhalator" },
            new Lijek { Naziv = "Pantoprazol", AtcKod = "A02BC02", Oblik = "kapsula" },
            new Lijek { Naziv = "Ibuprofen", AtcKod = "M01AE01", Oblik = "tableta" },
            new Lijek { Naziv = "Sertralin", AtcKod = "N06AB06", Oblik = "tableta" },
            new Lijek { Naziv = "Amoksicilin", AtcKod = "J01CA04", Oblik = "kapsula" },
            new Lijek { Naziv = "Hidrokortizon", AtcKod = "D07AA02", Oblik = "krema" }
        );

        await db.SaveChangesAsync();
        Console.WriteLine("Uneseni lijekovi (prvo pokretanje).");
    }
}