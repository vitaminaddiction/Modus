namespace Modus.Application.Multitenancy;

/// <summary>현재 요청의 테넌트. 미들웨어가 요청 시작 시 세팅한다(scoped).</summary>
public interface ITenantContext
{
    string? TenantCode { get; }
    bool HasTenant { get; }
    void SetTenant(string code);
}
