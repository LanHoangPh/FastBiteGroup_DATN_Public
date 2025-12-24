namespace FastBiteGroupMCA.Application.DTOs.Admin
{
    public class AnalyticsDto
    {
        public List<ChartDataItemDto> UserGrowthChartData { get; set; } = new();
        public List<ChartDataItemDto> GroupGrowthChartData { get; set; } = new();
        public List<ChartDataItemDto> VideoCallChartData { get; set; } = new();
        public List<ChartDataItemDto> PostGrowthChartData { get; set; } = new();
        public List<ChartDataItemDto> CommentGrowthChartData { get; set; } = new();

        public ClassificationChartsDto ClassificationCharts { get; set; } = new();


    }
    public class ChartItemDto
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
    }
    public class ChartDataItemDto
    {
        public string Date { get; set; } // Định dạng "yyyy-MM-dd"
        public int Count { get; set; }
    }

    // DTO cho các biểu đồ phân loại
    public class ClassificationChartsDto
    {
        public List<ChartItemDto> UserRoleDistribution { get; set; } = new();
        public List<ChartItemDto> UserStatusDistribution { get; set; } = new();
        public List<ChartItemDto> GroupTypeDistribution { get; set; } = new();
        public List<ChartItemDto> ReportStatusDistribution { get; set; } = new();
    }
}
