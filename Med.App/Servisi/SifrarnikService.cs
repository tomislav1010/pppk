using Med.Data.Ef;
using Med.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Med.App.Servisi;

public class SifrarnikService
{
    private readonly MedDbContext _db;

    public SifrarnikService(MedDbContext db) => _db = db;

    public async Task<List<Lijecnik>> LijecniciAsync() =>
        await _db.Lijecnici.OrderBy(l => l.Prezime).AsNoTracking().ToListAsync();

    public async Task<List<Dijagnoza>> DijagnozeAsync() =>
        await _db.Dijagnoze.OrderBy(d => d.Sifra).AsNoTracking().ToListAsync();

    public async Task<List<Lijek>> LijekoviAsync() =>
        await _db.Lijekovi.OrderBy(l => l.Naziv).AsNoTracking().ToListAsync();

    public async Task<List<Adresa>> AdreseAsync() =>
        await _db.Adrese.OrderBy(a => a.Grad).ThenBy(a => a.Ulica).AsNoTracking().ToListAsync();
}
