using Modus.Domain.Common;

namespace Modus.Domain.BaseCode;

/// <summary>공통코드그룹 (기초코드). 예: "품목유형", "거래처구분".</summary>
public class CodeGroup : CodeNameBase
{
    public string? Description { get; set; }

    public ICollection<Code> Codes { get; set; } = new List<Code>();
}
