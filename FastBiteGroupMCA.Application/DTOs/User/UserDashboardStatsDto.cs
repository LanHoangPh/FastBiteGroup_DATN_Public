namespace FastBiteGroupMCA.Application.DTOs.User
{
    public class UserDashboardStatsDto
    {
        /// <summary>
        /// Số tin nhắn người dùng đã gửi trong ngày hôm nay.
        /// </summary>
        public long MessagesTodayCount { get; set; }

        /// <summary>
        /// Tổng số nhóm người dùng đang tham gia.
        /// </summary>
        public int JoinedGroupsCount { get; set; }

        /// <summary>
        /// Số lượng người dùng duy nhất đã từng có cuộc trò chuyện 1-1.
        /// </summary>
        public int UniqueDirectChatPartnersCount { get; set; }
    }
}
