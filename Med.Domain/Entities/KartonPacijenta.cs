using Med.ORM.Mapping;

namespace Med.Domain.Entities;

[Table("kartoni_pacijenata")]
public class KartonPacijenta
{
    [PrimaryKey, Column("id")]
    public int Id { get; set; }

    [Column("pacijent_id"), NotNull, Unique, ForeignKey(typeof(Pacijent), OnDelete = "CASCADE")]
    public int PacijentId { get; set; }

    [Column("krvna_grupa", Type = SqlType.Char, Length = 3)]
    public string? KrvnaGrupa { get; set; }

    [Column("visina_cm", Type = SqlType.Float)]
    public double? VisinaCm { get; set; }

    [Column("tezina_kg", Type = SqlType.Float)]
    public double? TezinaKg { get; set; }

    [Column("alergije", Type = SqlType.Text)]
    public string? Alergije { get; set; }

    [Navigation(nameof(PacijentId))]
    public virtual Pacijent? Pacijent { get; set; }
}