using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Group;

public class AddMemberRequestDto
{
    [Required]
    public Guid UserIdToAdd { get; set; }
}
