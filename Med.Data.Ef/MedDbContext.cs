using Med.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Med.Data.Ef;

public class MedDbContext : DbContext
{
    public MedDbContext(DbContextOptions<MedDbContext> options) : base(options) { }

    public DbSet<Adresa> Adrese => Set<Adresa>();
    public DbSet<Pacijent> Pacijenti => Set<Pacijent>();
    public DbSet<KartonPacijenta> Kartoni => Set<KartonPacijenta>();
    public DbSet<Lijecnik> Lijecnici => Set<Lijecnik>();
    public DbSet<Dijagnoza> Dijagnoze => Set<Dijagnoza>();
    public DbSet<Lijek> Lijekovi => Set<Lijek>();
    public DbSet<PovijestBolesti> PovijestiBolesti => Set<PovijestBolesti>();
    public DbSet<Terapija> Terapije => Set<Terapija>();
    public DbSet<Pregled> Pregledi => Set<Pregled>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MedDbContext).Assembly);
    }
}