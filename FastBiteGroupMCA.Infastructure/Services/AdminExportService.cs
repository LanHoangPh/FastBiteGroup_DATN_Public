using ClosedXML.Excel;
using FastBiteGroupMCA.Application.DTOs.Admin;
using FastBiteGroupMCA.Application.DTOs.Admin.Group;
using FastBiteGroupMCA.Application.DTOs.Admin.User;
using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Infastructure.Services.FileStorage;
using Hangfire;
using Microsoft.AspNetCore.Identity;

namespace FastBiteGroupMCA.Infastructure.Services;

public class AdminExportService : IAdminExportService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly StorageStrategy _storageStrategy;
    private readonly ILogger<AdminExportService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAdminNotificationService _notificationService;
    public AdminExportService(
        UserManager<AppUser> userManager,
        StorageStrategy storageStrategy,
        IAdminNotificationService notificationService,
        ILogger<AdminExportService> logger,
        IUnitOfWork unitOfWork,
        RoleManager<AppRole> roleManager)
    {
        _userManager = userManager;
        _storageStrategy = storageStrategy;
        _notificationService = notificationService;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _roleManager = roleManager;
    }

    public async Task GenerateGroupsExportFileAsync(GetGroupsAdminParams filters, Guid adminId, string adminFullName)
    {
        var fileName = $"Export_Groups_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        var query = _unitOfWork.Groups.GetQueryable().IgnoreQueryFilters().AsNoTracking();
        query = ApplyGroupAdminFilters(query, filters);

        var groupsToExport = await query
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new AdminGroupExportDto
            {
                GroupId = g.GroupID,
                GroupName = g.GroupName,
                CreatorName = g.CreatedByUser!.FullName!,
                MemberCount = g.Members.Count(),
                GroupType = g.GroupType.ToString(),
                Status = g.IsDeleted ? "Đã xóa" : (g.IsArchived ? "Đã lưu trữ" : "Hoạt động"),
                CreatedAt = g.CreatedAt
            })
            .ToListAsync();
        if (filters.TimezoneOffsetMinutes.HasValue)
        {
            foreach (var group in groupsToExport)
            {
                group.CreatedAt = group.CreatedAt.AddMinutes(-filters.TimezoneOffsetMinutes.Value);
            }
        }

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Danh sách Group");
            var table = worksheet.Cell("A1").InsertTable(groupsToExport);

            // Sửa lỗi cú pháp: Dùng HeaderRow() (số ít)
            var headerRow = table.HeadersRow();
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            worksheet.Columns().AdjustToContents();

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var fileBytes = stream.ToArray();
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                var storageService = _storageStrategy.GetStorageService(contentType);
                var uploadResult = await storageService.UploadAsync(fileBytes, fileName, contentType, "exports");

                if (uploadResult.Success)
                {
                    await _notificationService.SendExportReadyNotificationAsync(adminId, fileName, uploadResult.Url);
                }
                else
                {
                    await _notificationService.SendExportFailedNotificationAsync(adminId, fileName, "Đã có lỗi hệ thống xảy ra.");
                }
            }
        }
    }

    //[AutomaticRetry(Attempts = 1)]
    public async Task GenerateUsersExportFileAsync(GetUsersAdminParams filters, Guid adminId, string adminFullName)
    {
        var fileName = $"Export_Users_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        try
        {
            var query = _userManager.Users.AsNoTracking();
            query = await ApplyUserAdminFilters(query, filters);
            var users = await query.OrderBy(u => u.FullName).ToListAsync();

            if (!users.Any())
            {
                await _notificationService.SendExportFailedNotificationAsync(adminId, fileName, "Đã có lỗi hệ thống xảy ra.");
                return;
            }

            // 2. Lấy Roles cho các user đã lọc trong 1 lần gọi DB
            var userIds = users.Select(u => u.Id).ToList();
            var rolesLookup = (await (from userRole in _unitOfWork.UserRoles.GetQueryable()
                                      join role in _unitOfWork.Roles.GetQueryable() on userRole.RoleId equals role.Id
                                      where userIds.Contains(userRole.UserId)
                                      select new { userRole.UserId, role.Name })
                                        .ToListAsync())
                                        .ToLookup(x => x.UserId, x => x.Name!);

            // 3. Tạo DTO "phẳng" để export
            var usersToExport = users.Select(u => new AdminUserExportDto
            {
                UserId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                UserName = u.UserName,
                Roles = string.Join(", ", rolesLookup[u.Id]),
                Status = u.IsActive ? "Hoạt động" : "Bị vô hiệu hóa",
                CreatedAt = u.CreatedAt
            }).ToList();

            if (filters.TimezoneOffsetMinutes.HasValue)
            {
                // Dùng vòng lặp để chuyển đổi múi giờ cho danh sách đã lấy về
                foreach (var user in usersToExport)
                {
                    user.CreatedAt = user.CreatedAt.AddMinutes(-filters.TimezoneOffsetMinutes.Value);
                }
            }

            // 2. Tạo và định dạng file Excel bằng ClosedXML
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Danh sách Người dùng");
                var table = worksheet.Cell("A1").InsertTable(usersToExport);

                // Sửa lỗi cú pháp: Dùng HeaderRow() (số ít)
                var headerRow = table.HeadersRow();
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var fileBytes = stream.ToArray();
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    var storageService = _storageStrategy.GetStorageService(contentType);
                    var uploadResult = await storageService.UploadAsync(fileBytes, fileName, contentType, "exports");

                    if (uploadResult.Success)
                    {
                        await _notificationService.SendExportReadyNotificationAsync(adminId, fileName, uploadResult.Url);
                    }
                    else
                    {
                        await _notificationService.SendExportFailedNotificationAsync(adminId, fileName, "Đã có lỗi hệ thống xảy ra.");
                    }
                }
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background job GenerateUsersExportFileAsync failed for Admin {AdminId}", adminId);
            await _notificationService.SendExportFailedNotificationAsync(adminId, fileName, "Đã có lỗi hệ thống xảy ra.");
            throw; 
        }
    }
    private async Task<IQueryable<AppUser>> ApplyUserAdminFilters(IQueryable<AppUser> query, GetUsersAdminParams filters)
    {
        // Lọc theo từ khóa tìm kiếm
        if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
        {
            var term = $"%{filters.SearchTerm.Trim()}%";
            query = query.Where(u =>
                (u.FullName != null && EF.Functions.Like(u.FullName, term)) ||
                (u.Email != null && EF.Functions.Like(u.Email, term))
            );
        }

        // Lọc theo trạng thái
        if (filters.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == filters.IsActive.Value);
        }

        // Lọc theo Vai trò (Role)
        if (!string.IsNullOrWhiteSpace(filters.Role))
        {
            var role = await _roleManager.FindByNameAsync(filters.Role);
            if (role != null)
            {
                // Dùng subquery để có hiệu năng tốt nhất
                query = query.Where(u => _unitOfWork.UserRoles.GetQueryable()
                                            .Any(ur => ur.UserId == u.Id && ur.RoleId == role.Id));
            }
            else
            {
                // Nếu role không tồn tại, trả về kết quả rỗng
                return Enumerable.Empty<AppUser>().AsQueryable();
            }
        }

        return query;
    }
    private IQueryable<Group> ApplyGroupAdminFilters(IQueryable<Group> query, GetGroupsAdminParams filters)
    {
        switch (filters.Status)
        {
            case GroupStatusFilter.Archived:
                query = query.Where(g => g.IsArchived && !g.IsDeleted);
                break;
            case GroupStatusFilter.Deleted:
                query = query.Where(g => g.IsDeleted);
                break;
            case GroupStatusFilter.Active:
                query = query.Where(g => !g.IsArchived && !g.IsDeleted);
                break;
            case GroupStatusFilter.All:
                break;
            default:
                query = query.Where(g => !g.IsArchived && !g.IsDeleted);
                break;
        }

        if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
        {
            var searchTerm = filters.SearchTerm.Trim().ToLower();
            query = query.Where(g => g.GroupName.ToLower().Contains(searchTerm));
        }

        if (filters.GroupType.HasValue)
        {
            query = filters.GroupType.Value switch
            {
                MyGroupFilterType.Chat => query.Where(g => g.GroupType == EnumGroupType.Public || g.GroupType == EnumGroupType.Private),
                MyGroupFilterType.Community => query.Where(g => g.GroupType == EnumGroupType.Community),
                _ => query 
            };
        }

        return query;
    }
    private IQueryable<AdminAuditLog> ApplyAuditLogFilters(IQueryable<AdminAuditLog> query, GetAdminAuditLogsParams filters)
    {
        if (filters.AdminId.HasValue)
            query = query.Where(log => log.AdminUserId == filters.AdminId.Value);

        if (filters.ActionType.HasValue)
            query = query.Where(log => log.ActionType == filters.ActionType.Value);

        if (filters.TargetEntityType.HasValue)
            query = query.Where(log => log.TargetEntityType == filters.TargetEntityType);

        if (!string.IsNullOrEmpty(filters.TargetEntityId))
            query = query.Where(log => log.TargetEntityId == filters.TargetEntityId);

        if (filters.StartDate.HasValue)
            query = query.Where(log => log.Timestamp >= filters.StartDate.Value);

        if (filters.EndDate.HasValue)
        {
            var endDateInclusive = filters.EndDate.Value.Date.AddDays(1);
            query = query.Where(log => log.Timestamp < endDateInclusive);
        }

        if (filters.BatchId.HasValue)
            query = query.Where(log => log.BatchId == filters.BatchId.Value);

        return query;
    }

    public async Task GenerateAuditLogsExportFileAsync(GetAdminAuditLogsParams filters, Guid adminId, string adminFullName)
    {
        var fileName = $"Export_AuditLogs_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        try
        {
            var query = _unitOfWork.AdminAuditLogs.GetQueryable().AsNoTracking();

            query = ApplyAuditLogFilters(query, filters);

            var logsToExport = await query
                .OrderByDescending(log => log.Timestamp)
                .Select(log => new AdminAuditLogExportDto
                {
                    Id = log.Id,
                    AdminFullName = log.AdminFullName,
                    ActionType = log.ActionType.ToString(),
                    TargetEntityType = log.TargetEntityType.ToString(),
                    TargetEntityId = log.TargetEntityId,
                    Details = log.Details,
                    Timestamp = log.Timestamp,
                    BatchId = log.BatchId
                })
                .ToListAsync();

            if (filters.TimezoneOffsetMinutes.HasValue)
            {
                // Dùng vòng lặp để chuyển đổi múi giờ cho danh sách đã lấy về
                foreach (var log in logsToExport)
                {
                    log.Timestamp = log.Timestamp.AddMinutes(-filters.TimezoneOffsetMinutes.Value);
                }
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Audit Logs");
                var table = worksheet.Cell("A1").InsertTable(logsToExport);
                // Sửa lỗi cú pháp: Dùng HeaderRow() (số ít)
                var headerRow = table.HeadersRow();
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;
                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var fileBytes = stream.ToArray();
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    var storageService = _storageStrategy.GetStorageService(contentType);
                    var uploadResult = await storageService.UploadAsync(fileBytes, fileName, contentType, "exports");
                    if (uploadResult.Success)
                    {
                        await _notificationService.SendExportReadyNotificationAsync(adminId, fileName, uploadResult.Url);
                    }
                    else
                    {
                        await _notificationService.SendExportFailedNotificationAsync(adminId, fileName, "Đã có lỗi hệ thống xảy ra.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background job GenerateAuditLogsExportFileAsync failed for Admin {AdminId}", adminId);
            await _notificationService.SendExportFailedNotificationAsync(adminId, fileName, "Đã có lỗi hệ thống xảy ra.");
            throw; // Ném lại lỗi để Hangfire đánh dấu job là "Failed"
        }
        
    }
}
