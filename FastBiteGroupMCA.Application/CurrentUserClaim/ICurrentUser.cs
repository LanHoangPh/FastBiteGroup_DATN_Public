namespace FastBiteGroupMCA.Application.CurrentUserClaim;
public interface ICurrentUser
{
    string? Id { get; }
    string? UserName { get; }
    string? Email { get; }
    string? FirstName { get; }
    string? LastName { get; }
    string? FullName { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }

    bool IsInRole(string roleName);
}
