using Med.ORM.Mapping;

namespace Med.Domain.Entities;

[Table("adrese")]
public class Adresa
{
    [PrimaryKey, Column("id")]
    public int Id { get; set; }

    [Column("ulica", Type = SqlType.Varchar, Length = 120), NotNull]
    public string Ulica { get; set; } = string.Empty;

    [Column("kucni_broj", Type = SqlType.Varchar, Length = 10)]
    public string? KucniBroj { get; set; }

    [Column("grad", Type = SqlType.Varchar, Length = 80), NotNull]
    public string Grad { get; set; } = string.Empty;

    [Column("postanski_broj", Type = SqlType.Char, Length = 5)]
    public string? PostanskiBroj { get; set; }

    [Column("drzava", Type = SqlType.Varchar, Length = 60), NotNull, Default("'Hrvatska'")]
    public string Drzava { get; set; } = "Hrvatska";

    public override string ToString() => $"{Ulica} {KucniBroj}, {PostanskiBroj} {Grad}";
}