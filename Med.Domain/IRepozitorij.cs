using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Med.Domain
{
    public interface IRepozitorij<T> where T : class
        {
            Task<IReadOnlyList<T>> DohvatiSveAsync(CancellationToken ct = default);
            Task<T?> DohvatiAsync(int id, CancellationToken ct = default);
            Task<IReadOnlyList<T>> PronadiAsync(Expression<Func<T, bool>> uvjet, CancellationToken ct = default);
            Task DodajAsync(T entitet, CancellationToken ct = default);
            void Azuriraj(T entitet);
            void Obrisi(T entitet);
            Task<int> SpremiPromjeneAsync(CancellationToken ct = default);
        }
    }

