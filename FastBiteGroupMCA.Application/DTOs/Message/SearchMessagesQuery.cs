using FastBiteGroupMCA.Application.DTOs.Common;
using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Message;

public class SearchMessagesQuery : PaginationParams
{
    [Required]
    public string Term { get; set; } = string.Empty;
}
