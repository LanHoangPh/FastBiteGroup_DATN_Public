namespace FastBiteGroupMCA.Application.DTOs.Group
{
    public class CreateInvitationRequestDTO
    {
        /// <summary>
        /// Link sẽ hết hạn sau bao nhiêu giờ. Bỏ trống nếu không có hạn.
        /// </summary>
        public int? ExpiresInHours { get; set; }

        /// <summary>
        /// Số lượt sử dụng tối đa. Bỏ trống nếu không giới hạn.
        /// </summary>
        public int? MaxUses { get; set; }
    }
}
