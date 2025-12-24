namespace FastBiteGroupMCA.Domain.Abstractions.Enities;

public interface IDateTracking
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
