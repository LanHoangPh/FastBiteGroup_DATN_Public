namespace FastBiteGroupMCA.Application.DTOs.User;

public class GetUserStatusesRequest
{
    public List<Guid> UserIds { get; set; } = new();
}
