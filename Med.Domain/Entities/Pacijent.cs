using Med.ORM.Mapping;

namespace Med.Domain.Entities;

[Table("pacijenti")]
public class Pacijent
{
    [PrimaryKey, Column("id")]
    public int Id { get; set; }

    [Column("ime", Type = SqlType.Varchar, Length = 60), NotNull]
    public string Ime { get; set; } = string.Empty;

    [Column("prezime", Type = SqlType.Varchar, Length = 80), NotNull]
    public string Prezime { get; set; } = string.Empty;

    [Column("oib", Type = SqlType.Char, Length = 11), NotNull, Unique]
    public string Oib { get; set; } = string.Empty;

    [Column("datum_rodenja", Type = SqlType.Timestamp), NotNull]
    public DateTime DatumRodenja { get; set; }

    [Column("spol", Type = SqlType.Char, Length = 1), NotNull]
    public char Spol { get; set; }

    [Column("telefon", Type = SqlType.Varchar, Length = 20)]
    public string? Telefon { get; set; }

    [Column("adresa_boravista_id"), NotNull, ForeignKey(typeof(Adresa))]
    public int AdresaBoravistaId { get; set; }

    [Column("adresa_prebivalista_id"), ForeignKey(typeof(Adresa), OnDelete = "SET NULL")]
    public int? AdresaPrebivalistaId { get; set; }

    [Column("kreirano_na", Type = SqlType.TimestampTz), NotNull, Default("now()")]
    public DateTimeOffset KreiranoNa { get; set; }

    [Navigation(nameof(AdresaBoravistaId))]
    public virtual Adresa? AdresaBoravista { get; set; }

    [Navigation(nameof(AdresaPrebivalistaId))]
    public virtual Adresa? AdresaPrebivalista { get; set; }

    [InverseNavigation("PacijentId")]
    public virtual KartonPacijenta? Karton { get; set; }

    [InverseNavigation("PacijentId")]
    public virtual ICollection<PovijestBolesti> PovijestBolesti { get; set; } = new List<PovijestBolesti>();

    [InverseNavigation("PacijentId")]
    public virtual ICollection<Terapija> Terapije { get; set; } = new List<Terapija>();

    [InverseNavigation("PacijentId")]
    public virtual ICollection<Pregled> Pregledi { get; set; } = new List<Pregled>();

    public override string ToString() => $"{Prezime}, {Ime} ({Oib})";
}