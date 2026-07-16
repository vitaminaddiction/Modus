using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modus.Application.Multitenancy;
using Modus.Infrastructure.Multitenancy;
using Modus.Infrastructure.Persistence;

namespace Modus.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// 카탈로그 DbContext + 멀티테넌시 인프라 + 테넌트별로 커넥션이 해석되는 ModusDbContext 등록.
    /// </summary>
    public static IServiceCollection AddModusInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var catalogCs = config.GetConnectionString("Catalog")
            ?? throw new InvalidOperationException("ConnectionStrings:Catalog 설정이 필요합니다.");

        // 카탈로그 DB (테넌트 레지스트리)
        services.AddDbContext<CatalogDbContext>(o =>
            o.UseNpgsql(catalogCs).UseSnakeCaseNamingConvention());

        // 멀티테넌시
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddSingleton<ITenantStore, TenantStore>();

        // 테넌트 데이터 DB — 커넥션스트링을 요청 시점에 현재 테넌트로 해석
        services.AddDbContext<ModusDbContext>((sp, o) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.HasTenant)
                throw new InvalidOperationException("요청에 테넌트가 해석되지 않았습니다(X-Tenant-Code/서브도메인/JWT).");

            var store = sp.GetRequiredService<ITenantStore>();
            var tenant = store.Find(tenantContext.TenantCode!)
                ?? throw new InvalidOperationException($"알 수 없는 테넌트 '{tenantContext.TenantCode}'.");

            o.UseNpgsql(TenantConnectionString.For(tenant)).UseSnakeCaseNamingConvention();
        });

        return services;
    }
}
