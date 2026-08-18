using Med.ORM.Mapping;

namespace Med.Domain.Entities;

[Table("lijekovi")]
public class Lijek
{
    [PrimaryKey, Column("id")]
    public int Id { get; set; }

    [Column("naziv", Type = SqlType.Varchar, Length = 150), NotNull]
    public string Naziv { get; set; } = string.Empty;

    [Column("atc_kod", Type = SqlType.Varchar, Length = 10)]
    public string? AtcKod { get; set; }

    [Column("oblik", Type = SqlType.Varchar, Length = 50), NotNull]
    public string Oblik { get; set; } = string.Empty;

    public override string ToString() => $"{Naziv} ({Oblik})";
}