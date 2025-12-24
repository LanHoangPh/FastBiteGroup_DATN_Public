namespace FastBiteGroupMCA.Domain.Abstractions.Enities;

public interface IUserTracking
{
    long CreatedBy { get; set; }
    long? LastModifiedBy { get; set; }
}
