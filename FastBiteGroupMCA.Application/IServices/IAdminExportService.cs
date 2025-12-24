using FastBiteGroupMCA.Application.DTOs.Admin;
using FastBiteGroupMCA.Application.DTOs.Admin.Group;
using FastBiteGroupMCA.Application.DTOs.Admin.User;

namespace FastBiteGroupMCA.Application.IServices;

public interface IAdminExportService
{
    Task GenerateUsersExportFileAsync(GetUsersAdminParams filters, Guid adminId, string adminFullName);
    Task GenerateGroupsExportFileAsync(GetGroupsAdminParams filters, Guid adminId, string adminFullName);
    Task GenerateAuditLogsExportFileAsync(GetAdminAuditLogsParams filters, Guid adminId, string adminFullName);
}
