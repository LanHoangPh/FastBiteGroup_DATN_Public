using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Post;

public class UpdatePinStatusDto
{
    [Required]
    public bool IsPinned { get; set; }
}
