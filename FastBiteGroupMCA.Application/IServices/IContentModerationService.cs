using System.ComponentModel;

namespace FastBiteGroupMCA.Application.IServices;

public interface IContentModerationService
{
    [DisplayName("Moderate content for PostId: {0}")]
    Task ModeratePostAsync(int postId);
}
