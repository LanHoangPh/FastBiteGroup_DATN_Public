using FastBiteGroupMCA.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Admin.Group
{
    public class BulkGroupActionRequest
    {
        [Required]
        public EnumBulkGroupActionType Action { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất một nhóm.")]
        public List<Guid> GroupIds { get; set; } = new();
    }
}
