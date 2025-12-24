using Microsoft.AspNetCore.Authorization;

namespace FastBiteGroupMCA.Application.Authorization;

public class GroupOperations
{
    public static readonly ManageGroupRequirement View = new() { Name = nameof(View) };
    public static readonly ManageGroupRequirement CreateContent = new() { Name = nameof(CreateContent) };   
    public static readonly ManageGroupRequirement RemoveMember = new() { Name = nameof(RemoveMember) };
    public static readonly ManageGroupRequirement EditInfo = new() { Name = nameof(EditInfo) };
    public static readonly ManageGroupRequirement ManageRoles = new() { Name = nameof(ManageRoles) };
    public static readonly ManageGroupRequirement DeleteGroup = new() { Name = nameof(DeleteGroup) };
    public static readonly ManageGroupRequirement ModerateContent = new() { Name = nameof(ModerateContent) };
}
public class ManageGroupRequirement : IAuthorizationRequirement
{
    public string Name { get; set; } = string.Empty;
}
