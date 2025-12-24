namespace FastBiteGroupMCA.Domain.Abstractions.Enities;

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}
