using Med.Data.Ef;
using Med.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Med.App.Servisi;

public class SifrarnikService
{
    private readonly MedDbContext _db;

    public SifrarnikService(MedDbContext db) => _db = db;

    // ---------- citanje ----------

    public async Task<List<Lijecnik>> LijecniciAsync() =>
        await _db.Lijecnici.OrderBy(l => l.Prezime).AsNoTracking().ToListAsync();

    public async Task<List<Dijagnoza>> DijagnozeAsync() =>
        await _db.Dijagnoze.OrderBy(d => d.Sifra).AsNoTracking().ToListAsync();

    public async Task<List<Lijek>> LijekoviAsync() =>
        await _db.Lijekovi.OrderBy(l => l.Naziv).AsNoTracking().ToListAsync();

    public async Task<List<Adresa>> AdreseAsync() =>
        await _db.Adrese.OrderBy(a => a.Grad).ThenBy(a => a.Ulica).AsNoTracking().ToListAsync();

    public async Task<Dijagnoza?> DijagnozaAsync(int id) => await _db.Dijagnoze.FindAsync(id);

    public async Task<Lijek?> LijekAsync(int id) => await _db.Lijekovi.FindAsync(id);

    public async Task<Adresa?> AdresaAsync(int id) => await _db.Adrese.FindAsync(id);

    // ---------- dijagnoze ----------

    public async Task<(bool uspjeh, string poruka)> DodajDijagnozuAsync(Dijagnoza dijagnoza)
    {
        var greska = await ProvjeriDijagnozuAsync(dijagnoza);
        if (greska is not null) return (false, greska);

        _db.Dijagnoze.Add(dijagnoza);
        await _db.SaveChangesAsync();
        return (true, $"Dijagnoza {dijagnoza.Sifra} spremljena.");
    }

    public async Task<(bool uspjeh, string poruka)> AzurirajDijagnozuAsync(Dijagnoza dijagnoza)
    {
        var greska = await ProvjeriDijagnozuAsync(dijagnoza);
        if (greska is not null) return (false, greska);

        await _db.SaveChangesAsync();
        return (true, "Dijagnoza azurirana.");
    }

    public async Task<(bool uspjeh, string poruka)> ObrisiDijagnozuAsync(int id)
    {
        var dijagnoza = await _db.Dijagnoze.FindAsync(id);
        if (dijagnoza is null) return (false, "Dijagnoza nije pronadena.");

        // Veza je Restrict, pa bi brisanje inace puklo na razini baze.
        var koristi = await _db.PovijestiBolesti.CountAsync(pb => pb.DijagnozaId == id);
        if (koristi > 0)
            return (false, $"Dijagnoza se koristi u {koristi} zapisa povijesti bolesti i ne moze se obrisati.");

        _db.Dijagnoze.Remove(dijagnoza);
        await _db.SaveChangesAsync();
        return (true, "Dijagnoza obrisana.");
    }

    private async Task<string?> ProvjeriDijagnozuAsync(Dijagnoza dijagnoza)
    {
        if (string.IsNullOrWhiteSpace(dijagnoza.Sifra)) return "Sifra je obavezna.";
        if (dijagnoza.Sifra.Length > 10) return "Sifra smije imati najvise 10 znakova.";
        if (string.IsNullOrWhiteSpace(dijagnoza.Naziv)) return "Naziv je obavezan.";

        if (await _db.Dijagnoze.AnyAsync(d => d.Sifra == dijagnoza.Sifra && d.Id != dijagnoza.Id))
            return $"Dijagnoza sa sifrom {dijagnoza.Sifra} vec postoji.";

        return null;
    }

    // ---------- lijekovi ----------

    public async Task<(bool uspjeh, string poruka)> DodajLijekAsync(Lijek lijek)
    {
        var greska = ProvjeriLijek(lijek);
        if (greska is not null) return (false, greska);

        _db.Lijekovi.Add(lijek);
        await _db.SaveChangesAsync();
        return (true, $"Lijek {lijek.Naziv} spremljen.");
    }

    public async Task<(bool uspjeh, string poruka)> AzurirajLijekAsync(Lijek lijek)
    {
        var greska = ProvjeriLijek(lijek);
        if (greska is not null) return (false, greska);

        await _db.SaveChangesAsync();
        return (true, "Lijek azuriran.");
    }

    public async Task<(bool uspjeh, string poruka)> ObrisiLijekAsync(int id)
    {
        var lijek = await _db.Lijekovi.FindAsync(id);
        if (lijek is null) return (false, "Lijek nije pronaden.");

        var koristi = await _db.Terapije.CountAsync(t => t.LijekId == id);
        if (koristi > 0)
            return (false, $"Lijek se koristi u {koristi} terapija i ne moze se obrisati.");

        _db.Lijekovi.Remove(lijek);
        await _db.SaveChangesAsync();
        return (true, "Lijek obrisan.");
    }

    private static string? ProvjeriLijek(Lijek lijek)
    {
        if (string.IsNullOrWhiteSpace(lijek.Naziv)) return "Naziv je obavezan.";
        if (string.IsNullOrWhiteSpace(lijek.Oblik)) return "Oblik je obavezan.";
        if (lijek.AtcKod is { Length: > 10 }) return "ATC kod smije imati najvise 10 znakova.";
        return null;
    }

    // ---------- adrese ----------

    public async Task<(bool uspjeh, string poruka)> DodajAdresuAsync(Adresa adresa)
    {
        var greska = ProvjeriAdresu(adresa);
        if (greska is not null) return (false, greska);

        _db.Adrese.Add(adresa);
        await _db.SaveChangesAsync();
        return (true, "Adresa spremljena.");
    }

    public async Task<(bool uspjeh, string poruka)> AzurirajAdresuAsync(Adresa adresa)
    {
        var greska = ProvjeriAdresu(adresa);
        if (greska is not null) return (false, greska);

        await _db.SaveChangesAsync();
        return (true, "Adresa azurirana.");
    }

    public async Task<(bool uspjeh, string poruka)> ObrisiAdresuAsync(int id)
    {
        var adresa = await _db.Adrese.FindAsync(id);
        if (adresa is null) return (false, "Adresa nije pronadena.");

        // Boraviste je Restrict, prebivaliste SET NULL - blokira samo prvo.
        var boravista = await _db.Pacijenti.CountAsync(p => p.AdresaBoravistaId == id);
        if (boravista > 0)
            return (false, $"Adresa je boraviste {boravista} pacijenata i ne moze se obrisati.");

        var prebivalista = await _db.Pacijenti.CountAsync(p => p.AdresaPrebivalistaId == id);

        _db.Adrese.Remove(adresa);
        await _db.SaveChangesAsync();

        return prebivalista > 0
            ? (true, $"Adresa obrisana. {prebivalista} pacijenata je ostalo bez prebivalista.")
            : (true, "Adresa obrisana.");
    }

    public async Task<int> BrojKorisnikaAdreseAsync(int id) =>
        await _db.Pacijenti.CountAsync(
            p => p.AdresaBoravistaId == id || p.AdresaPrebivalistaId == id);

    /// Broj pacijenata po adresi, jednim upitom - da ispis popisa ne bi radio N+1.
    public async Task<Dictionary<int, int>> BrojeviKorisnikaAdresaAsync()
    {
        var boravista = await _db.Pacijenti
            .GroupBy(p => p.AdresaBoravistaId)
            .Select(g => new { Id = g.Key, Broj = g.Count() })
            .ToListAsync();

        var prebivalista = await _db.Pacijenti
            .Where(p => p.AdresaPrebivalistaId != null)
            .GroupBy(p => p.AdresaPrebivalistaId!.Value)
            .Select(g => new { Id = g.Key, Broj = g.Count() })
            .ToListAsync();

        var brojevi = new Dictionary<int, int>();
        foreach (var stavka in boravista.Concat(prebivalista))
            brojevi[stavka.Id] = brojevi.GetValueOrDefault(stavka.Id) + stavka.Broj;

        return brojevi;
    }

    private static string? ProvjeriAdresu(Adresa adresa)
    {
        if (string.IsNullOrWhiteSpace(adresa.Ulica)) return "Ulica je obavezna.";
        if (string.IsNullOrWhiteSpace(adresa.Grad)) return "Grad je obavezan.";
        if (string.IsNullOrWhiteSpace(adresa.Drzava)) return "Drzava je obavezna.";
        if (adresa.PostanskiBroj is { Length: > 5 }) return "Postanski broj smije imati najvise 5 znakova.";
        return null;
    }
}
