using Modus.Domain.Common;

namespace Modus.Domain.Catalog;

/// <summary>
/// 카탈로그 DB에 저장되는 테넌트 레지스트리. 테넌트 DB 접속정보를 보유.
/// (DB-per-tenant: 각 테넌트의 실제 데이터는 DbName이 가리키는 별도 DB에 있음)
/// </summary>
public class Tenant : EntityBase
{
    /// <summary>테넌트 식별 코드 (서브도메인 / X-Tenant-Code 헤더 / JWT tenant 클레임).</summary>
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string DbName { get; set; } = default!;
    public string DbUser { get; set; } = default!;
    public string DbPassword { get; set; } = default!;

    public bool Enabled { get; set; } = true;
}
