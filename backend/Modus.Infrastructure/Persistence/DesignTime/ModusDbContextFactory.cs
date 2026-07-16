using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modus.Infrastructure.Persistence.DesignTime;

/// <summary>
/// `dotnet ef migrations`용 테넌트 컨텍스트 팩토리. 마이그레이션 "생성"은 접속하지 않으므로
/// 커넥션스트링은 형식만 유효하면 된다(적용은 Modus.Tools 마이그레이터가 테넌트별로 수행).
/// </summary>
public class ModusDbContextFactory : IDesignTimeDbContextFactory<ModusDbContext>
{
    public ModusDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("MODUS_TENANT_CS")
            ?? "Host=localhost;Port=5433;Database=modus_t_demo;Username=modus;Password=modus_dev_pw";
        var options = new DbContextOptionsBuilder<ModusDbContext>()
            .UseNpgsql(cs).UseSnakeCaseNamingConvention().Options;
        return new ModusDbContext(options);
    }
}
