using Med.App.Servisi;
using Med.Domain.Entities;

namespace Med.App.Izbornici;

public class Odabir
{
    private readonly PacijentService _pacijenti;
    private readonly SifrarnikService _sifrarnik;

    public Odabir(PacijentService pacijenti, SifrarnikService sifrarnik)
    {
        _pacijenti = pacijenti;
        _sifrarnik = sifrarnik;
    }

    public async Task<Pacijent?> PacijentAsync()
    {
        var lista = await _pacijenti.DohvatiSveAsync();
        if (lista.Count == 0)
        {
            Ui.Info("Nema pacijenata u bazi. Prvo dodaj pacijenta.");
            Ui.Pauza();
            return null;
        }

        return Ui.Odaberi("Odaberi pacijenta:", lista, p => $"{p.Prezime}, {p.Ime} ({p.Oib})");
    }

    public async Task<Lijecnik?> LijecnikAsync(string oznaka = "Odaberi lijecnika:")
    {
        var lista = await _sifrarnik.LijecniciAsync();
        if (lista.Count == 0)
        {
            Ui.Greska("Nema lijecnika. Seed nije izvrsen.");
            Ui.Pauza();
            return null;
        }

        return Ui.Odaberi(oznaka, lista, l => l.ToString());
    }

    public async Task<Dijagnoza?> DijagnozaAsync()
    {
        var lista = await _sifrarnik.DijagnozeAsync();
        if (lista.Count == 0)
        {
            Ui.Greska("Nema dijagnoza. Seed nije izvrsen.");
            Ui.Pauza();
            return null;
        }

        return Ui.Odaberi("Odaberi dijagnozu:", lista, d => d.ToString());
    }

    public async Task<Lijek?> LijekAsync()
    {
        var lista = await _sifrarnik.LijekoviAsync();
        if (lista.Count == 0)
        {
            Ui.Greska("Nema lijekova. Seed nije izvrsen.");
            Ui.Pauza();
            return null;
        }

        return Ui.Odaberi("Odaberi lijek:", lista, l => l.ToString());
    }
}
