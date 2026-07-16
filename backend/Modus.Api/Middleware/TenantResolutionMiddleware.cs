using Modus.Application.Multitenancy;

namespace Modus.Api.Middleware;

/// <summary>
/// 요청에서 테넌트를 해석해 ITenantContext에 세팅.
/// 우선순위: X-Tenant-Code 헤더 → JWT 'tenant' 클레임 → 서브도메인.
/// (JWT 클레임을 쓰려면 UseAuthentication 뒤에 배치할 것)
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx, ITenantContext tenantContext)
    {
        var code = Resolve(ctx);
        if (!string.IsNullOrWhiteSpace(code))
            tenantContext.SetTenant(code!);

        await _next(ctx);
    }

    private static string? Resolve(HttpContext ctx)
    {
        if (ctx.Request.Headers.TryGetValue("X-Tenant-Code", out var header) &&
            !string.IsNullOrWhiteSpace(header))
            return header.ToString().Trim();

        var claim = ctx.User?.FindFirst("tenant")?.Value;
        if (!string.IsNullOrWhiteSpace(claim))
            return claim;

        var host = ctx.Request.Host.Host;
        var parts = host.Split('.');
        if (parts.Length > 2)            // acme.mes.example.com → "acme"
            return parts[0];

        return null;
    }
}
