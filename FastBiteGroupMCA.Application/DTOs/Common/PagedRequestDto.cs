using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Common
{
    public class PagedRequestDto
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 10;

        [StringLength(100)]
        public string? SearchTerm { get; set; }
    }
}
