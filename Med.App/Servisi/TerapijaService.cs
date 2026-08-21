using Med.Data.Ef;
using Med.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Med.App.Servisi;

public class TerapijaService
{
    private readonly MedDbContext _db;

    public TerapijaService(MedDbContext db) => _db = db;

    public async Task<List<Terapija>> DohvatiZaPacijentaAsync(int pacijentId) =>
        await _db.Terapije
            .Include(t => t.Lijek)
            .Include(t => t.Lijecnik)
            .Include(t => t.PovijestBolesti).ThenInclude(pb => pb!.Dijagnoza)
            .Where(t => t.PacijentId == pacijentId)
            .OrderByDescending(t => t.DatumOd)
            .AsNoTracking()
            .ToListAsync();

    public async Task<Terapija?> DohvatiAsync(int id) =>
        await _db.Terapije.FindAsync(id);

    public async Task DodajAsync(Terapija terapija)
    {
        _db.Terapije.Add(terapija);
        await _db.SaveChangesAsync();
    }

    public async Task SpremiPromjeneAsync() => await _db.SaveChangesAsync();

    public async Task<bool> ObrisiAsync(int id)
    {
        var t = await _db.Terapije.FindAsync(id);
        if (t is null) return false;

        _db.Terapije.Remove(t);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task ZavrsiAsync(int id)
    {
        var t = await _db.Terapije.FindAsync(id);
        if (t is null) return;

        t.Aktivna = false;
        t.DatumDo = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);
        await _db.SaveChangesAsync();
    }
}