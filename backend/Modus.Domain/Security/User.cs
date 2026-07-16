using Modus.Domain.Common;

namespace Modus.Domain.Security;

/// <summary>테넌트 DB 내 사용자 계정. 로그인은 (테넌트 + LoginId + 비밀번호) 조합.</summary>
public class User : EntityBase
{
    public string LoginId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;

    /// <summary>역할. 초기엔 "Admin" / "User" 정도. 세밀 권한은 후순위.</summary>
    public string Role { get; set; } = "User";

    public string? Email { get; set; }
    public bool Enabled { get; set; } = true;
}
