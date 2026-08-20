using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Med.Domain.Entities;
using Med.Data.Ef;
using Microsoft.EntityFrameworkCore;

namespace Med.App
{
    public static class Seeder
    {
        public static async Task PokreniAsync(MedDbContext db)
        {
            if (await db.Lijecnici.AnyAsync())
                return;

            db.Lijecnici.AddRange(
                new Lijecnik { Ime = "Ana", Prezime = "Horvat", Specijalizacija = "Obiteljska medicina" },
                new Lijecnik { Ime = "Marko", Prezime = "Kovac", Specijalizacija = "Kardiologija" },
                new Lijecnik { Ime = "Ivana", Prezime = "Novak", Specijalizacija = "Radiologija" },
                new Lijecnik { Ime = "Petar", Prezime = "Babic", Specijalizacija = "Neurologija" },
                new Lijecnik { Ime = "Lucija", Prezime = "Maric", Specijalizacija = "Dermatologija" }
            );

            await db.SaveChangesAsync();
            Console.WriteLine("Lijecnici uneseni pri prvom pokretanju.");
        }
    }
}
