using Med.ORM.Mapping;

namespace Med.Domain.Entities;

[Table("dijagnoze")]
public class Dijagnoza
{
    [PrimaryKey, Column("id")]
    public int Id { get; set; }

    [Column("sifra", Type = SqlType.Varchar, Length = 10), NotNull, Unique]
    public string Sifra { get; set; } = string.Empty;

    [Column("naziv", Type = SqlType.Varchar, Length = 200), NotNull]
    public string Naziv { get; set; } = string.Empty;

    [Column("opis", Type = SqlType.Text)]
    public string? Opis { get; set; }

    public override string ToString() => $"{Sifra} - {Naziv}";
}