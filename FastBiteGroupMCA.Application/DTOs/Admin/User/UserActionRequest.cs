using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Admin.User;

public class UserActionRequest
{
    [Required] public byte[] RowVersion { get; set; }
}
