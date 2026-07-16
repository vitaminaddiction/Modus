using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modus.Infrastructure.Persistence.DesignTime;

/// <summary>`dotnet ef migrations`용 카탈로그 컨텍스트 팩토리. 개발 기본값(도커 PG) 또는 env로 오버라이드.</summary>
public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("MODUS_CATALOG_CS")
            ?? "Host=localhost;Port=5433;Database=modus_catalog;Username=modus;Password=modus_dev_pw";
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(cs).UseSnakeCaseNamingConvention().Options;
        return new CatalogDbContext(options);
    }
}
