namespace FastBiteGroupMCA.Domain.Abstractions.Enities;

public interface IAuditable : IDateTracking, IUserTracking, ISoftDelete
{
}
