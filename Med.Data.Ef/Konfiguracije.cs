using Med.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Med.Data.Ef.Konfiguracije;

public class AdresaKonfiguracija : IEntityTypeConfiguration<Adresa>
{
    public void Configure(EntityTypeBuilder<Adresa> b)
    {
        b.ToTable("adrese");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.Ulica).HasColumnName("ulica").HasColumnType("varchar(120)").IsRequired();
        b.Property(x => x.KucniBroj).HasColumnName("kucni_broj").HasColumnType("varchar(10)");
        b.Property(x => x.Grad).HasColumnName("grad").HasColumnType("varchar(80)").IsRequired();
        b.Property(x => x.PostanskiBroj).HasColumnName("postanski_broj").HasColumnType("char(5)");
        b.Property(x => x.Drzava).HasColumnName("drzava").HasColumnType("varchar(60)")
            .IsRequired().HasDefaultValue("Hrvatska");
    }
}

public class PacijentKonfiguracija : IEntityTypeConfiguration<Pacijent>
{
    public void Configure(EntityTypeBuilder<Pacijent> b)
    {
        b.ToTable("pacijenti");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.Ime).HasColumnName("ime").HasColumnType("varchar(60)").IsRequired();
        b.Property(x => x.Prezime).HasColumnName("prezime").HasColumnType("varchar(80)").IsRequired();
        b.Property(x => x.Oib).HasColumnName("oib").HasColumnType("char(11)").IsRequired();
        b.Property(x => x.DatumRodenja).HasColumnName("datum_rodenja")
            .HasColumnType("timestamp without time zone").IsRequired();
        b.Property(x => x.Spol).HasColumnName("spol").HasColumnType("char(1)").IsRequired();
        b.Property(x => x.AdresaBoravistaId).HasColumnName("adresa_boravista_id").IsRequired();
        b.Property(x => x.AdresaPrebivalistaId).HasColumnName("adresa_prebivalista_id");
        b.Property(x => x.KreiranoNa).HasColumnName("kreirano_na")
            .HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

        b.HasIndex(x => x.Oib).IsUnique();

        b.HasOne(x => x.AdresaBoravista).WithMany()
            .HasForeignKey(x => x.AdresaBoravistaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AdresaPrebivalista).WithMany()
            .HasForeignKey(x => x.AdresaPrebivalistaId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class KartonKonfiguracija : IEntityTypeConfiguration<KartonPacijenta>
{
    public void Configure(EntityTypeBuilder<KartonPacijenta> b)
    {
        b.ToTable("kartoni_pacijenata");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.PacijentId).HasColumnName("pacijent_id").IsRequired();
        b.Property(x => x.KrvnaGrupa).HasColumnName("krvna_grupa").HasColumnType("char(3)");
        b.Property(x => x.VisinaCm).HasColumnName("visina_cm").HasColumnType("double precision");
        b.Property(x => x.TezinaKg).HasColumnName("tezina_kg").HasColumnType("double precision");
        b.Property(x => x.Alergije).HasColumnName("alergije").HasColumnType("text");

        b.HasIndex(x => x.PacijentId).IsUnique();

        b.HasOne(x => x.Pacijent).WithOne(p => p.Karton)
            .HasForeignKey<KartonPacijenta>(x => x.PacijentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class LijecnikKonfiguracija : IEntityTypeConfiguration<Lijecnik>
{
    public void Configure(EntityTypeBuilder<Lijecnik> b)
    {
        b.ToTable("lijecnici");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.Ime).HasColumnName("ime").HasColumnType("varchar(60)").IsRequired();
        b.Property(x => x.Prezime).HasColumnName("prezime").HasColumnType("varchar(80)").IsRequired();
        b.Property(x => x.Specijalizacija).HasColumnName("specijalizacija")
            .HasColumnType("varchar(100)").IsRequired();
    }
}

public class DijagnozaKonfiguracija : IEntityTypeConfiguration<Dijagnoza>
{
    public void Configure(EntityTypeBuilder<Dijagnoza> b)
    {
        b.ToTable("dijagnoze");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.Sifra).HasColumnName("sifra").HasColumnType("varchar(10)").IsRequired();
        b.Property(x => x.Naziv).HasColumnName("naziv").HasColumnType("varchar(200)").IsRequired();
        b.Property(x => x.Opis).HasColumnName("opis").HasColumnType("text");

        b.HasIndex(x => x.Sifra).IsUnique();
    }
}

public class LijekKonfiguracija : IEntityTypeConfiguration<Lijek>
{
    public void Configure(EntityTypeBuilder<Lijek> b)
    {
        b.ToTable("lijekovi");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.Naziv).HasColumnName("naziv").HasColumnType("varchar(150)").IsRequired();
        b.Property(x => x.AtcKod).HasColumnName("atc_kod").HasColumnType("varchar(10)");
        b.Property(x => x.Oblik).HasColumnName("oblik").HasColumnType("varchar(50)").IsRequired();
    }
}

public class PovijestBolestiKonfiguracija : IEntityTypeConfiguration<PovijestBolesti>
{
    public void Configure(EntityTypeBuilder<PovijestBolesti> b)
    {
        b.ToTable("povijest_bolesti");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.PacijentId).HasColumnName("pacijent_id").IsRequired();
        b.Property(x => x.DijagnozaId).HasColumnName("dijagnoza_id").IsRequired();
        b.Property(x => x.LijecnikId).HasColumnName("lijecnik_id").IsRequired();
        b.Property(x => x.DatumOd).HasColumnName("datum_od")
            .HasColumnType("timestamp without time zone").IsRequired();
        b.Property(x => x.DatumDo).HasColumnName("datum_do")
            .HasColumnType("timestamp without time zone");
        b.Property(x => x.Napomena).HasColumnName("napomena").HasColumnType("text");

        b.HasOne(x => x.Pacijent).WithMany(p => p.PovijestBolesti)
            .HasForeignKey(x => x.PacijentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Dijagnoza).WithMany()
            .HasForeignKey(x => x.DijagnozaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Lijecnik).WithMany()
            .HasForeignKey(x => x.LijecnikId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TerapijaKonfiguracija : IEntityTypeConfiguration<Terapija>
{
    public void Configure(EntityTypeBuilder<Terapija> b)
    {
        b.ToTable("terapije");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.PacijentId).HasColumnName("pacijent_id").IsRequired();
        b.Property(x => x.LijekId).HasColumnName("lijek_id").IsRequired();
        b.Property(x => x.PovijestBolestiId).HasColumnName("povijest_bolesti_id");
        b.Property(x => x.LijecnikId).HasColumnName("lijecnik_id").IsRequired();
        b.Property(x => x.Doza).HasColumnName("doza").HasColumnType("decimal(10,2)").IsRequired();
        b.Property(x => x.JedinicaDoze).HasColumnName("jedinica_doze")
            .HasColumnType("varchar(20)").IsRequired();
        b.Property(x => x.Ucestalost).HasColumnName("ucestalost")
            .HasColumnType("varchar(60)").IsRequired();
        b.Property(x => x.DatumOd).HasColumnName("datum_od")
            .HasColumnType("timestamp without time zone").IsRequired();
        b.Property(x => x.DatumDo).HasColumnName("datum_do")
            .HasColumnType("timestamp without time zone");
        b.Property(x => x.Aktivna).HasColumnName("aktivna").HasDefaultValue(true);

        b.HasOne(x => x.Pacijent).WithMany(p => p.Terapije)
            .HasForeignKey(x => x.PacijentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Lijek).WithMany()
            .HasForeignKey(x => x.LijekId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.PovijestBolesti).WithMany(p => p.Terapije)
            .HasForeignKey(x => x.PovijestBolestiId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Lijecnik).WithMany()
            .HasForeignKey(x => x.LijecnikId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PregledKonfiguracija : IEntityTypeConfiguration<Pregled>
{
    public void Configure(EntityTypeBuilder<Pregled> b)
    {
        b.ToTable("pregledi");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.PacijentId).HasColumnName("pacijent_id").IsRequired();
        b.Property(x => x.LijecnikId).HasColumnName("lijecnik_id").IsRequired();
        b.Property(x => x.UputiteljId).HasColumnName("uputitelj_id");
        b.Property(x => x.Tip).HasColumnName("tip").HasColumnType("varchar(10)")
            .HasConversion<string>().IsRequired();
        b.Property(x => x.Termin).HasColumnName("termin")
            .HasColumnType("timestamp with time zone").IsRequired();
        b.Property(x => x.TrajanjeMinuta).HasColumnName("trajanje_minuta").HasDefaultValue(30);
        b.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(20)")
            .HasConversion<string>().IsRequired().HasDefaultValueSql("'Zakazan'");
        b.Property(x => x.Nalaz).HasColumnName("nalaz").HasColumnType("text");

        b.HasOne(x => x.Pacijent).WithMany(p => p.Pregledi)
            .HasForeignKey(x => x.PacijentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Lijecnik).WithMany(l => l.Pregledi)
            .HasForeignKey(x => x.LijecnikId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Uputitelj).WithMany()
            .HasForeignKey(x => x.UputiteljId).OnDelete(DeleteBehavior.SetNull);
    }
}