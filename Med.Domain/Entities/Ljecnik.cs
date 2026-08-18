using Med.ORM.Mapping;

namespace Med.Domain.Entities;

[Table("lijecnici")]
public class Lijecnik
{
    [PrimaryKey, Column("id")]
    public int Id { get; set; }

    [Column("ime", Type = SqlType.Varchar, Length = 60), NotNull]
    public string Ime { get; set; } = string.Empty;

    [Column("prezime", Type = SqlType.Varchar, Length = 80), NotNull]
    public string Prezime { get; set; } = string.Empty;

    [Column("specijalizacija", Type = SqlType.Varchar, Length = 100), NotNull]
    public string Specijalizacija { get; set; } = string.Empty;

    [InverseNavigation("LijecnikId")]
    public virtual ICollection<Pregled> Pregledi { get; set; } = new List<Pregled>();

    public override string ToString() => $"dr. {Ime} {Prezime} ({Specijalizacija})";
}