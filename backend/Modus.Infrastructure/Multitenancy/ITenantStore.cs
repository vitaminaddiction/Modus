using Modus.Domain.Catalog;

namespace Modus.Infrastructure.Multitenancy;

/// <summary>테넌트 코드 → 테넌트 조회(카탈로그 DB, 캐시).</summary>
public interface ITenantStore
{
    Tenant? Find(string code);
    void Invalidate(string code);
}
