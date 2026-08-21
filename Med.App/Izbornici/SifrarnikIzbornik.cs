using Med.App.Servisi;
using Spectre.Console;

namespace Med.App.Izbornici;

public class SifrarnikIzbornik
{
    private readonly SifrarnikService _sifrarnik;

    public SifrarnikIzbornik(SifrarnikService sifrarnik) => _sifrarnik = sifrarnik;

    public async Task PokreniAsync()
    {
        while (true)
        {
            Ui.Naslov("Sifrarnici");

            var izbor = Ui.Odaberi("Odaberi popis:", new[]
            {
                "Lijecnici",
                "Dijagnoze (MKB-10)",
                "Lijekovi",
                "Natrag"
            }, x => x);

            switch (izbor)
            {
                case "Lijecnici": await LijecniciAsync(); break;
                case "Dijagnoze (MKB-10)": await DijagnozeAsync(); break;
                case "Lijekovi": await LijekoviAsync(); break;
                default: return;
            }
        }
    }

    private async Task LijecniciAsync()
    {
        Ui.Naslov("Lijecnici");
        Ui.Info("Lijecnici se ne ureduju kroz CRUD - unose se pri prvom pokretanju aplikacije.\n");

        var tablica = new Table().Border(TableBorder.Rounded);
        tablica.AddColumn("Id");
        tablica.AddColumn("Prezime, ime");
        tablica.AddColumn("Specijalizacija");

        foreach (var l in await _sifrarnik.LijecniciAsync())
            tablica.AddRow(l.Id.ToString(), $"{l.Prezime}, {l.Ime}", l.Specijalizacija);

        AnsiConsole.Write(tablica);
        Ui.Pauza();
    }

    private async Task DijagnozeAsync()
    {
        Ui.Naslov("Dijagnoze");

        var tablica = new Table().Border(TableBorder.Rounded);
        tablica.AddColumn("Id");
        tablica.AddColumn("Sifra");
        tablica.AddColumn("Naziv");
        tablica.AddColumn("Opis");

        foreach (var d in await _sifrarnik.DijagnozeAsync())
            tablica.AddRow(d.Id.ToString(), d.Sifra, d.Naziv, d.Opis ?? "-");

        AnsiConsole.Write(tablica);
        Ui.Pauza();
    }

    private async Task LijekoviAsync()
    {
        Ui.Naslov("Lijekovi");

        var tablica = new Table().Border(TableBorder.Rounded);
        tablica.AddColumn("Id");
        tablica.AddColumn("Naziv");
        tablica.AddColumn("ATC kod");
        tablica.AddColumn("Oblik");

        foreach (var l in await _sifrarnik.LijekoviAsync())
            tablica.AddRow(l.Id.ToString(), l.Naziv, l.AtcKod ?? "-", l.Oblik);

        AnsiConsole.Write(tablica);
        Ui.Pauza();
    }
}
