using Modus.Application.Multitenancy;

namespace Modus.Infrastructure.Multitenancy;

/// <summary>요청 범위(scoped) 테넌트 컨텍스트 구현.</summary>
public sealed class TenantContext : ITenantContext
{
    public string? TenantCode { get; private set; }
    public bool HasTenant => !string.IsNullOrEmpty(TenantCode);
    public void SetTenant(string code) => TenantCode = code;
}
