using Med.Data.Ef;
using Med.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Med.App.Servisi;

public class PregledService
{
    private readonly MedDbContext _db;

    public PregledService(MedDbContext db) => _db = db;

    public async Task<List<Pregled>> DohvatiZaPacijentaAsync(int pacijentId) =>
        await _db.Pregledi
            .Include(p => p.Lijecnik)
            .Include(p => p.Uputitelj)
            .Where(p => p.PacijentId == pacijentId)
            .OrderByDescending(p => p.Termin)
            .AsNoTracking()
            .ToListAsync();

    public async Task<List<Pregled>> DohvatiNadolazeceAsync() =>
        await _db.Pregledi
            .Include(p => p.Pacijent)
            .Include(p => p.Lijecnik)
            .Where(p => p.Termin >= DateTimeOffset.UtcNow && p.Status == StatusPregleda.Zakazan)
            .OrderBy(p => p.Termin)
            .AsNoTracking()
            .ToListAsync();

    public async Task<Pregled?> DohvatiAsync(int id) =>
        await _db.Pregledi.FindAsync(id);

    public async Task<(bool uspjeh, string poruka)> ZakaziAsync(Pregled pregled)
    {
        var kraj = pregled.Termin.AddMinutes(pregled.TrajanjeMinuta);

        var zauzet = await _db.Pregledi.AnyAsync(p =>
            p.LijecnikId == pregled.LijecnikId &&
            p.Status == StatusPregleda.Zakazan &&
            p.Termin < kraj &&
            pregled.Termin < p.Termin.AddMinutes(p.TrajanjeMinuta));

        if (zauzet)
            return (false, "Lijecnik je u tom terminu zauzet.");

        _db.Pregledi.Add(pregled);
        await _db.SaveChangesAsync();
        return (true, "Pregled zakazan.");
    }

    public async Task SpremiPromjeneAsync() => await _db.SaveChangesAsync();

    public async Task<bool> ObrisiAsync(int id)
    {
        var p = await _db.Pregledi.FindAsync(id);
        if (p is null) return false;

        _db.Pregledi.Remove(p);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task PromijeniStatusAsync(int id, StatusPregleda status, string? nalaz = null)
    {
        var p = await _db.Pregledi.FindAsync(id);
        if (p is null) return;

        p.Status = status;
        if (nalaz is not null) p.Nalaz = nalaz;

        await _db.SaveChangesAsync();
    }
}