using Modus.Domain.Common;

namespace Modus.Domain.BaseCode;

/// <summary>공통코드 (기초코드). 특정 CodeGroup에 속함.</summary>
public class Code : CodeNameBase
{
    public long CodeGroupId { get; set; }
    public CodeGroup? CodeGroup { get; set; }

    public int SortOrder { get; set; }
    public string? Value { get; set; }
    public string? Description { get; set; }
}
