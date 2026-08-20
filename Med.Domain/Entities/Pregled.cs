using Med.ORM.Mapping;

namespace Med.Domain.Entities;

[Table("pregledi")]
public class Pregled
{
    [PrimaryKey, Column("id")]
    public int Id { get; set; }

    [Column("pacijent_id"), NotNull, ForeignKey(typeof(Pacijent), OnDelete = "CASCADE")]
    public int PacijentId { get; set; }

    [Column("lijecnik_id"), NotNull, ForeignKey(typeof(Lijecnik))]
    public int LijecnikId { get; set; }

    [Column("uputitelj_id"), ForeignKey(typeof(Lijecnik), OnDelete = "SET NULL")]
    public int? UputiteljId { get; set; }

    [Column("tip", Type = SqlType.Varchar, Length = 10), NotNull]
    public TipPregleda Tip { get; set; }

    [Column("termin", Type = SqlType.TimestampTz), NotNull]
    public DateTimeOffset Termin { get; set; }

    [Column("trajanje_minuta", Type = SqlType.Int), NotNull, Default("30")]
    public int TrajanjeMinuta { get; set; } = 30;

    [Column("status", Type = SqlType.Varchar, Length = 20), NotNull, Default("'Zakazan'")]
    public StatusPregleda Status { get; set; } = StatusPregleda.Zakazan;

    [Column("nalaz", Type = SqlType.Text)]
    public string? Nalaz { get; set; }

    [Navigation(nameof(PacijentId))]
    public virtual Pacijent? Pacijent { get; set; }

    [Navigation(nameof(LijecnikId))]
    public virtual Lijecnik? Lijecnik { get; set; }

    [Navigation(nameof(UputiteljId))]
    public virtual Lijecnik? Uputitelj { get; set; }
}