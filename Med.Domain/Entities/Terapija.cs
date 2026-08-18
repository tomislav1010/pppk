using Med.ORM.Mapping;

namespace Med.Domain.Entities;

[Table("terapije")]
public class Terapija
{
    [PrimaryKey, Column("id")]
    public int Id { get; set; }

    [Column("pacijent_id"), NotNull, ForeignKey(typeof(Pacijent), OnDelete = "CASCADE")]
    public int PacijentId { get; set; }

    [Column("lijek_id"), NotNull, ForeignKey(typeof(Lijek))]
    public int LijekId { get; set; }

    [Column("povijest_bolesti_id"), ForeignKey(typeof(PovijestBolesti), OnDelete = "SET NULL")]
    public int? PovijestBolestiId { get; set; }

    [Column("lijecnik_id"), NotNull, ForeignKey(typeof(Lijecnik))]
    public int LijecnikId { get; set; }

    [Column("doza", Type = SqlType.Decimal, Precision = 10, Scale = 2), NotNull]
    public decimal Doza { get; set; }

    [Column("jedinica_doze", Type = SqlType.Varchar, Length = 20), NotNull]
    public string JedinicaDoze { get; set; } = string.Empty;

    [Column("ucestalost", Type = SqlType.Varchar, Length = 60), NotNull]
    public string Ucestalost { get; set; } = string.Empty;

    [Column("datum_od", Type = SqlType.Timestamp), NotNull]
    public DateTime DatumOd { get; set; }

    [Column("datum_do", Type = SqlType.Timestamp)]
    public DateTime? DatumDo { get; set; }

    [Column("aktivna", Type = SqlType.Boolean), NotNull, Default("true")]
    public bool Aktivna { get; set; } = true;

    [Navigation(nameof(PacijentId))]
    public virtual Pacijent? Pacijent { get; set; }

    [Navigation(nameof(LijekId))]
    public virtual Lijek? Lijek { get; set; }

    [Navigation(nameof(PovijestBolestiId))]
    public virtual PovijestBolesti? PovijestBolesti { get; set; }

    [Navigation(nameof(LijecnikId))]
    public virtual Lijecnik? Lijecnik { get; set; }
}