using System.Linq.Expressions;
using Med.Domain;
using Microsoft.EntityFrameworkCore;

namespace Med.Data.Ef;

public class EfRepozitorij<T> : IRepozitorij<T> where T : class
{
    private readonly MedDbContext _db;
    private readonly DbSet<T> _set;

    public EfRepozitorij(MedDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    public async Task<IReadOnlyList<T>> DohvatiSveAsync(CancellationToken ct = default)
        => await _set.ToListAsync(ct);

    public async Task<T?> DohvatiAsync(int id, CancellationToken ct = default)
        => await _set.FindAsync(new object?[] { id }, ct);

    public async Task<IReadOnlyList<T>> PronadiAsync(
        Expression<Func<T, bool>> uvjet, CancellationToken ct = default)
        => await _set.Where(uvjet).ToListAsync(ct);

    public async Task DodajAsync(T entitet, CancellationToken ct = default)
        => await _set.AddAsync(entitet, ct);

    public void Azuriraj(T entitet) => _set.Update(entitet);

    public void Obrisi(T entitet) => _set.Remove(entitet);

    public async Task<int> SpremiPromjeneAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}