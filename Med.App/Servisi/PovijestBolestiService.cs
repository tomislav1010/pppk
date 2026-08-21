using Med.Data.Ef;
using Med.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Med.App.Servisi;

public class PovijestBolestiService
{
    private readonly MedDbContext _db;

    public PovijestBolestiService(MedDbContext db) => _db = db;

    public async Task<List<PovijestBolesti>> DohvatiZaPacijentaAsync(int pacijentId) =>
        await _db.PovijestiBolesti
            .Include(pb => pb.Dijagnoza)
            .Include(pb => pb.Lijecnik)
            .Where(pb => pb.PacijentId == pacijentId)
            .OrderByDescending(pb => pb.DatumOd)
            .AsNoTracking()
            .ToListAsync();

    public async Task<PovijestBolesti?> DohvatiAsync(int id) =>
        await _db.PovijestiBolesti.FindAsync(id);

    public async Task<(bool uspjeh, string poruka)> DodajAsync(PovijestBolesti zapis)
    {
        if (zapis.DatumDo is not null && zapis.DatumDo < zapis.DatumOd)
            return (false, "Datum zavrsetka ne moze biti prije datuma pocetka.");

        _db.PovijestiBolesti.Add(zapis);
        await _db.SaveChangesAsync();
        return (true, "Zapis povijesti bolesti spremljen.");
    }

    public async Task<(bool uspjeh, string poruka)> SpremiPromjeneAsync(PovijestBolesti zapis)
    {
        if (zapis.DatumDo is not null && zapis.DatumDo < zapis.DatumOd)
            return (false, "Datum zavrsetka ne moze biti prije datuma pocetka.");

        await _db.SaveChangesAsync();
        return (true, "Zapis azuriran.");
    }

    public async Task<(bool uspjeh, string poruka)> ObrisiAsync(int id)
    {
        var zapis = await _db.PovijestiBolesti.FindAsync(id);
        if (zapis is null) return (false, "Zapis nije pronaden.");

        var vezaneTerapije = await _db.Terapije.CountAsync(t => t.PovijestBolestiId == id);

        _db.PovijestiBolesti.Remove(zapis);
        await _db.SaveChangesAsync();

        return vezaneTerapije > 0
            ? (true, $"Zapis obrisan. {vezaneTerapije} terapija je ostalo bez povezane dijagnoze.")
            : (true, "Zapis obrisan.");
    }
}
