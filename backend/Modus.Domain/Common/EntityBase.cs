namespace Modus.Domain.Common;

/// <summary>모든 엔티티의 공통 베이스. Id + 감사 필드.</summary>
public abstract class EntityBase
{
    public long Id { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
