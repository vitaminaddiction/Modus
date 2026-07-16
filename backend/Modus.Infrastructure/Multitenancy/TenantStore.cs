using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modus.Domain.Catalog;
using Modus.Infrastructure.Persistence;

namespace Modus.Infrastructure.Multitenancy;

/// <summary>
/// 카탈로그 DB에서 테넌트를 조회하고 캐싱한다(싱글턴).
/// 요청마다 카탈로그를 때리지 않도록 코드→테넌트를 메모리에 보관.
/// </summary>
public sealed class TenantStore : ITenantStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<string, Tenant> _cache = new(StringComparer.OrdinalIgnoreCase);

    public TenantStore(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public Tenant? Find(string code)
    {
        if (_cache.TryGetValue(code, out var cached))
            return cached;

        using var scope = _scopeFactory.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var tenant = catalog.Tenants.AsNoTracking()
            .FirstOrDefault(t => t.Code == code && t.Enabled);

        if (tenant is not null)
            _cache[code] = tenant;

        return tenant;
    }

    public void Invalidate(string code) => _cache.TryRemove(code, out _);
}
