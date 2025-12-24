using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Admin.Group;

public class ChangeGroupOwnerDTO
{
    [Required]
    public Guid NewOwnerUserId { get; set; }
}
