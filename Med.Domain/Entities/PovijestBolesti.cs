using Med.ORM.Mapping;

namespace Med.Domain.Entities;

[Table("povijest_bolesti")]
public class PovijestBolesti
{
    [PrimaryKey, Column("id")]
    public int Id { get; set; }

    [Column("pacijent_id"), NotNull, ForeignKey(typeof(Pacijent), OnDelete = "CASCADE")]
    public int PacijentId { get; set; }

    [Column("dijagnoza_id"), NotNull, ForeignKey(typeof(Dijagnoza))]
    public int DijagnozaId { get; set; }

    [Column("lijecnik_id"), NotNull, ForeignKey(typeof(Lijecnik))]
    public int LijecnikId { get; set; }

    [Column("datum_od", Type = SqlType.Timestamp), NotNull]
    public DateTime DatumOd { get; set; }

    [Column("datum_do", Type = SqlType.Timestamp)]
    public DateTime? DatumDo { get; set; }

    [Column("napomena", Type = SqlType.Text)]
    public string? Napomena { get; set; }

    [Navigation(nameof(PacijentId))]
    public virtual Pacijent? Pacijent { get; set; }

    [Navigation(nameof(DijagnozaId))]
    public virtual Dijagnoza? Dijagnoza { get; set; }

    [Navigation(nameof(LijecnikId))]
    public virtual Lijecnik? Lijecnik { get; set; }

    [InverseNavigation("PovijestBolestiId")]
    public virtual ICollection<Terapija> Terapije { get; set; } = new List<Terapija>();
}