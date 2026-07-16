using Microsoft.EntityFrameworkCore;
using Modus.Domain.Catalog;

namespace Modus.Infrastructure.Persistence;

/// <summary>카탈로그 DB(modus_catalog): 테넌트 레지스트리 전용.</summary>
public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        var t = b.Entity<Tenant>();
        t.ToTable("tenant");
        t.HasKey(x => x.Id);
        t.HasIndex(x => x.Code).IsUnique();
        t.Property(x => x.Code).HasMaxLength(50).IsRequired();
        t.Property(x => x.Name).HasMaxLength(200).IsRequired();
        t.Property(x => x.Host).HasMaxLength(200).IsRequired();
        t.Property(x => x.DbName).HasMaxLength(100).IsRequired();
        t.Property(x => x.DbUser).HasMaxLength(100).IsRequired();
        t.Property(x => x.DbPassword).HasMaxLength(200).IsRequired();
    }
}
