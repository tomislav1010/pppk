using Med.App.Servisi;
using Med.Data.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

namespace Med.App.Izbornici;

public class GlavniIzbornik
{
    private readonly IConfiguration _config;
    private readonly MedDbContext _db;

    public GlavniIzbornik(IConfiguration config, MedDbContext db)
    {
        _config = config;
        _db = db;
    }

    public async Task PokreniAsync()
    {
        var sifrarnik = new SifrarnikService(_db);
        var pacijenti = new PacijentService(_db);
        var povijest = new PovijestBolestiService(_db);
        var terapije = new TerapijaService(_db);
        var pregledi = new PregledService(_db);
        var odabir = new Odabir(pacijenti, sifrarnik);

        var pacijentIzbornik = new PacijentIzbornik(pacijenti);
        var povijestIzbornik = new PovijestBolestiIzbornik(povijest, odabir);
        var terapijaIzbornik = new TerapijaIzbornik(terapije, povijest, odabir);
        var pregledIzbornik = new PregledIzbornik(pregledi, odabir);
        var sifrarnikIzbornik = new SifrarnikIzbornik(sifrarnik);
        var karton = new KartonPrikaz(pacijenti, odabir);
        var demo = new UcitavanjeDemo(_config);

        while (true)
        {
            Zaglavlje();

            var izbor = Ui.Odaberi("Glavni izbornik:", new[]
            {
                "Pacijenti",
                "Povijest bolesti",
                "Terapije",
                "Pregledi",
                "Sifrarnici",
                "Puni karton pacijenta",
                "Eager vs lazy loading",
                "Ucitaj demo podatke",
                "Izlaz"
            }, x => x);

            switch (izbor)
            {
                case "Pacijenti": await pacijentIzbornik.PokreniAsync(); break;
                case "Povijest bolesti": await povijestIzbornik.PokreniAsync(); break;
                case "Terapije": await terapijaIzbornik.PokreniAsync(); break;
                case "Pregledi": await pregledIzbornik.PokreniAsync(); break;
                case "Sifrarnici": await sifrarnikIzbornik.PokreniAsync(); break;
                case "Puni karton pacijenta": await karton.PokreniAsync(); break;
                case "Eager vs lazy loading": await demo.PokreniAsync(); break;
                case "Ucitaj demo podatke": await DemoAsync(); break;
                default: return;
            }
        }
    }

    private void Zaglavlje()
    {
        Ui.Ocisti();
        AnsiConsole.Write(new FigletText("MedSustav").Color(Color.Teal));
        AnsiConsole.MarkupLine($"[grey]Baza: {Baza.NazivKonfiguracije(_config)}[/]");
        AnsiConsole.WriteLine();
    }

    private async Task DemoAsync()
    {
        Ui.Naslov("Demo podaci");

        var poruka = await DemoPodaci.UcitajAsync(_db);
        Ui.Uspjeh(poruka);

        // demo je punio kroz isti kontekst, pa oslobadamo pracene instance
        _db.ChangeTracker.Clear();
        Ui.Pauza();
    }
}
