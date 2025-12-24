using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Message;

public class GetMessageContextQuery
{
    [Required]
    public string MessageId { get; set; } = string.Empty;

    // Số lượng tin nhắn muốn lấy ở mỗi phía (trước và sau)
    public int PageSize { get; set; } = 10;
}
