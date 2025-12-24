namespace FastBiteGroupMCA.Application.DTOs.Admin.Group;

public class GetAuditLogsParams
{
    public Guid? AdminId { get; set; }
    public string? ActionType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
