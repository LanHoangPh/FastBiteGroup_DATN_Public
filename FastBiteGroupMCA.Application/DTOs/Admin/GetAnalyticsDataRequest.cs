namespace FastBiteGroupMCA.Application.DTOs.Admin
{
    public class GetAnalyticsDataRequest
    {
        // Mặc định là 30 ngày nếu không được chỉ định
        public EnumTimeRange TimeRange { get; set; } = EnumTimeRange.Last30Days;
    }
    public enum EnumTimeRange
    {
        Last7Days,
        Last30Days,
        Last6Months,
        Last12Months
    }
}
