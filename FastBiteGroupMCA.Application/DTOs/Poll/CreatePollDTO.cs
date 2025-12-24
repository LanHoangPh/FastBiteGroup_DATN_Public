namespace FastBiteGroupMCA.Application.DTOs.Poll
{
    public class CreatePollDTO
    {
        public string Question { get; set; } = default!;
        public List<string> Options { get; set; } = new();
        /// <summary>
        /// (Tùy chọn) Thời gian cuộc bình chọn sẽ tự động đóng.
        /// </summary>
        public DateTime? ClosesAt { get; set; }
        /// <summary>
        /// Cho phép người dùng chọn nhiều lựa chọn hay không.
        /// </summary>
        public bool AllowMultipleChoices { get; set; }
    }
}
