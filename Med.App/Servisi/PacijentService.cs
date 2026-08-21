using Med.Data.Ef;
using Med.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Med.App.Servisi;

public class PacijentService
{
    private readonly MedDbContext _db;

    public PacijentService(MedDbContext db) => _db = db;

    public async Task<List<Pacijent>> DohvatiSveAsync() =>
        await _db.Pacijenti
            .Include(p => p.AdresaBoravista)
            .OrderBy(p => p.Prezime).ThenBy(p => p.Ime)
            .AsNoTracking()
            .ToListAsync();

    public async Task<Pacijent?> DohvatiAsync(int id) =>
        await _db.Pacijenti.FindAsync(id);

    public async Task<Pacijent?> DohvatiPunAsync(int id) =>
        await _db.Pacijenti
            .Include(p => p.AdresaBoravista)
            .Include(p => p.AdresaPrebivalista)
            .Include(p => p.Karton)
            .Include(p => p.PovijestBolesti).ThenInclude(pb => pb.Dijagnoza)
            .Include(p => p.Terapije).ThenInclude(t => t.Lijek)
            .Include(p => p.Pregledi).ThenInclude(pr => pr.Lijecnik)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<Pacijent>> PretraziAsync(string pojam)
    {
        pojam = pojam.Trim().ToLower();
        return await _db.Pacijenti
            .Where(p => p.Ime.ToLower().Contains(pojam)
                     || p.Prezime.ToLower().Contains(pojam)
                     || p.Oib.Contains(pojam))
            .OrderBy(p => p.Prezime)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<(bool uspjeh, string poruka)> DodajAsync(
        Pacijent pacijent, Adresa boraviste, Adresa? prebivaliste = null)
    {
        var greska = ValidirajOib(pacijent.Oib);
        if (greska is not null) return (false, greska);

        if (await _db.Pacijenti.AnyAsync(p => p.Oib == pacijent.Oib))
            return (false, "Pacijent s tim OIB-om vec postoji.");

        _db.Adrese.Add(boraviste);
        if (prebivaliste is not null) _db.Adrese.Add(prebivaliste);
        await _db.SaveChangesAsync();

        pacijent.AdresaBoravistaId = boraviste.Id;
        pacijent.AdresaPrebivalistaId = prebivaliste?.Id;
        pacijent.KreiranoNa = DateTimeOffset.UtcNow;

        _db.Pacijenti.Add(pacijent);
        await _db.SaveChangesAsync();

        return (true, $"Pacijent {pacijent.Ime} {pacijent.Prezime} spremljen.");
    }

    public async Task<(bool uspjeh, string poruka)> AzurirajAsync(Pacijent pacijent)
    {
        var greska = ValidirajOib(pacijent.Oib);
        if (greska is not null) return (false, greska);

        if (await _db.Pacijenti.AnyAsync(p => p.Oib == pacijent.Oib && p.Id != pacijent.Id))
            return (false, "Drugi pacijent vec koristi taj OIB.");

        await _db.SaveChangesAsync();
        return (true, "Podaci azurirani.");
    }

    public async Task<(bool uspjeh, string poruka)> ObrisiAsync(int id)
    {
        var pacijent = await _db.Pacijenti.FindAsync(id);
        if (pacijent is null) return (false, "Pacijent nije pronaden.");

        _db.Pacijenti.Remove(pacijent);
        await _db.SaveChangesAsync();
        return (true, "Pacijent obrisan.");
    }

    public async Task PostaviKartonAsync(int pacijentId, KartonPacijenta karton)
    {
        var postojeci = await _db.Kartoni.FirstOrDefaultAsync(k => k.PacijentId == pacijentId);

        if (postojeci is null)
        {
            karton.PacijentId = pacijentId;
            _db.Kartoni.Add(karton);
        }
        else
        {
            postojeci.KrvnaGrupa = karton.KrvnaGrupa;
            postojeci.VisinaCm = karton.VisinaCm;
            postojeci.TezinaKg = karton.TezinaKg;
            postojeci.Alergije = karton.Alergije;
        }

        await _db.SaveChangesAsync();
    }

    public static string? ValidirajOib(string? oib)
    {
        if (string.IsNullOrWhiteSpace(oib)) return "OIB je obavezan.";
        if (oib.Length != 11) return "OIB mora imati tocno 11 znamenki.";
        if (!oib.All(char.IsDigit)) return "OIB smije sadrzavati samo znamenke.";
        return null;
    }
}