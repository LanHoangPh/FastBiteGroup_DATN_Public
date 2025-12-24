using FastBiteGroupMCA.Domain.Abstractions.Enities;

namespace FastBiteGroupMCA.Domain.Abstractions;

public abstract class AuditTableBase : EntityBase, IAuditable
{
    public  DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long CreatedBy { get ; set ; }
    public long? LastModifiedBy { get ; set ; }
    public bool IsDeleted { get ; set ; }
    public DateTime? DeletedDate { get ; set ; }
    public long? DeletedBy { get ; set ; }
}
