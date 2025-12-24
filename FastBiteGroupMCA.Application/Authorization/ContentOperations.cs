using Microsoft.AspNetCore.Authorization;

namespace FastBiteGroupMCA.Application.Authorization
{
    public class ContentOperations
    {
        public static readonly ManageContentRequirement Edit = new() { Name = nameof(Edit) };
        public static readonly ManageContentRequirement Delete = new() { Name = nameof(Delete) };
    }
    public class ManageContentRequirement : IAuthorizationRequirement
    {
        public string Name { get; set; } = string.Empty;
    }
}
