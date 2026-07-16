using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Modus.Api.Auth;
using Modus.Api.Contracts;
using Modus.Application.Multitenancy;
using Modus.Application.Security;
using Modus.Infrastructure.Persistence;

namespace Modus.Api.Controllers.Web;

[ApiController]
[Route("api/web/auth")]
public class WebAuthController : ControllerBase
{
    private readonly ModusDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPasswordHasher _hasher;
    private readonly JwtTokenService _jwt;

    public WebAuthController(ModusDbContext db, ITenantContext tenant, IPasswordHasher hasher, JwtTokenService jwt)
    {
        _db = db;
        _tenant = tenant;
        _hasher = hasher;
        _jwt = jwt;
    }

    /// <summary>로그인. 테넌트는 X-Tenant-Code 헤더로 지정.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<UserInfo>> Login(LoginRequest req)
    {
        if (!_tenant.HasTenant)
            return BadRequest("테넌트가 지정되지 않았습니다(X-Tenant-Code 헤더).");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.LoginId == req.LoginId && u.Enabled);
        if (user is null || !_hasher.Verify(req.Password, user.PasswordHash))
            return Unauthorized("아이디 또는 비밀번호가 올바르지 않습니다.");

        var (token, expires) = _jwt.Create(user, _tenant.TenantCode!);
        AuthCookies.Issue(Response, token, expires);
        return new UserInfo(user.Id, user.LoginId, user.Name, user.Role, _tenant.TenantCode!);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        AuthCookies.Clear(Response);
        return NoContent();
    }

    /// <summary>현재 로그인 사용자.</summary>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<UserInfo> Me()
    {
        long.TryParse(User.FindFirst("sub")?.Value, out var id);
        return new UserInfo(
            id,
            User.FindFirst("loginId")?.Value ?? "",
            User.FindFirst(ClaimTypes.Name)?.Value ?? "",
            User.FindFirst(ClaimTypes.Role)?.Value ?? "",
            User.FindFirst("tenant")?.Value ?? "");
    }
}
