namespace Modus.Domain.Common;

/// <summary>코드/이름/사용여부를 갖는 표준 마스터 베이스. 대부분의 기초코드 화면이 이걸 상속.</summary>
public abstract class CodeNameBase : EntityBase
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool Enabled { get; set; } = true;
}
