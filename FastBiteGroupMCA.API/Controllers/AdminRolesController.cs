using FastBiteGroupMCA.Application.CurrentUserClaim;
using FastBiteGroupMCA.Application.DTOs.Admin.Role;
using FastBiteGroupMCA.Application.DTOs.Role;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.Response;
using FastBiteGroupMCA.Domain.Abstractions.Repository.EFCore;
using FastBiteGroupMCA.Domain.Entities.Identity;
using FastBiteGroupMCA.Domain.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBiteGroupMCA.API.Controllers;

[Route("api/v1/admin/roles")]
[ApiController]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "Admin-v1")]
public class AdminRolesController : ControllerBase
{
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IAdminAuditLogService _auditLogService; 
    private readonly ICurrentUser _currentUser;            
    private readonly UserManager<AppUser> _userManager; 
    private readonly List<string> _nonDeletableRoles = new() { "Admin" };
    private readonly IUnitOfWork _unitOfWork;

    public AdminRolesController(RoleManager<AppRole> roleManager, IAdminAuditLogService adminAuditLogService, ICurrentUser currentUser, UserManager<AppUser> userManager, IUnitOfWork unitOfWork)
    {
        _roleManager = roleManager;
        _auditLogService = adminAuditLogService;
        _currentUser = currentUser;
        _userManager = userManager;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// [Admin] Lấy danh sách tất cả các vai trò trong hệ thống.
    /// </summary>
    /// <remarks>
    /// Trả về danh sách các vai trò có sẵn để sử dụng khi gán quyền cho người dùng.
    /// </remarks>
    /// <response code="200">Lấy danh sách vai trò thành công.</response>
    /// <response code="401">Chưa xác thực.</response>
    /// <response code="403">Không có quyền Admin.</response>
    [HttpGet] // Chỉ cần 1 attribute [HttpGet] cho route gốc
    [ProducesResponseType(typeof(ApiResponse<List<RoleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetRoles()
    {
        var roleCounts = await _unitOfWork.UserRoles.GetQueryable() 
            .GroupBy(ur => ur.RoleId)
            .Select(g => new {
                RoleId = g.Key,
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count); 

        var allRoles = await _roleManager.Roles
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name!,
                IsSystemRole = r.IsSystemRole
            })
            .ToListAsync();

        foreach (var roleDto in allRoles)
        {
            roleDto.UserCount = roleCounts.GetValueOrDefault(roleDto.Id, 0);
        }
        var sortedRoles = allRoles.OrderByDescending(r => r.UserCount).ToList();

        return Ok(ApiResponse<List<RoleDto>>.Ok(sortedRoles, "Lấy danh sách vai trò thành công."));
    }
    /// <summary>
    /// [Admin] Tạo một vai trò mới trong hệ thống.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        // 1. Lấy thông tin admin thực hiện
        if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
        {
            return BadRequest(ApiResponse<object>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin."));
        }

        if (await _roleManager.RoleExistsAsync(request.RoleName))
            return BadRequest(ApiResponse<object>.Fail("ROLE_EXISTS", "Vai trò đã tồn tại."));

        var result = await _roleManager.CreateAsync(new AppRole { Name = request.RoleName, IsSystemRole = false });
        if (!result.Succeeded)
            return BadRequest(ApiResponse<object>.Fail("CREATE_ROLE_FAILED", "Tạo vai trò thất bại."));

        await _auditLogService.LogAdminActionAsync(adminId, _currentUser.FullName,
            EnumAdminActionType.RoleCreated, EnumTargetEntityType.Role, request.RoleName, $"Admin đã tạo vai trò mới: '{request.RoleName}'.");

        return StatusCode(201, ApiResponse<object>.Ok(null, "Tạo vai trò thành công."));
    }

    /// <summary>
    /// [Admin] Cập nhật tên của một vai trò.
    /// </summary>
    [HttpPut("{roleId:guid}")]
    public async Task<IActionResult> UpdateRole(Guid roleId, [FromBody] UpdateRoleRequest request)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
        {
            return BadRequest(ApiResponse<object>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin."));
        }
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role == null)

            return NotFound(ApiResponse<object>.Fail("ROLE_NOT_FOUND", "Không tìm thấy vai trò."));

        if (_nonDeletableRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.Fail("CANNOT_UPDATE_SYSTEM_ROLE", "Không thể cập nhật vai trò cốt lõi của hệ thống."));

        var roleWithNewName = await _roleManager.FindByNameAsync(request.NewRoleName);
        if (roleWithNewName != null && roleWithNewName.Id != roleId)
        {
            return BadRequest(ApiResponse<object>.Fail("ROLE_NAME_TAKEN", "Tên vai trò mới đã tồn tại."));
        }
        if (role.IsSystemRole)
        {
            return BadRequest(ApiResponse<object>.Fail("CANNOT_DELETE_SYSTEM_ROLE", "Không thể xóa vai trò cốt lõi của hệ thống."));
        }

        var oldRoleName = role.Name;
        role.Name = request.NewRoleName;
        var result = await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
            return BadRequest(ApiResponse<object>.Fail("UPDATE_ROLE_FAILED", "Cập nhật vai trò thất bại."));

        await _auditLogService.LogAdminActionAsync(adminId, _currentUser.FullName,
            EnumAdminActionType.RoleUpdated, EnumTargetEntityType.Role, roleId.ToString(), $"Admin đã đổi tên vai trò từ '{oldRoleName}' thành '{request.NewRoleName}'.");

        return Ok(ApiResponse<object>.Ok(null, "Cập nhật vai trò thành công."));
    }

    /// <summary>
    /// [Admin] Xóa một vai trò khỏi hệ thống.
    /// </summary>
    [HttpDelete("{roleId:guid}")]
    public async Task<IActionResult> DeleteRole(Guid roleId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
        {
            return BadRequest(ApiResponse<object>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin."));
        }
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role == null)
            return NotFound(ApiResponse<object>.Fail("ROLE_NOT_FOUND", "Không tìm thấy vai trò."));

        if (_nonDeletableRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<object>.Fail("CANNOT_DELETE_SYSTEM_ROLE", "Không thể xóa vai trò cốt lõi của hệ thống."));
        }
        if (role.IsSystemRole)
        {
            return BadRequest(ApiResponse<object>.Fail("CANNOT_DELETE_SYSTEM_ROLE", "Không thể xóa vai trò cốt lõi của hệ thống."));
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
        if (usersInRole.Any())
            return BadRequest(ApiResponse<object>.Fail("ROLE_IN_USE", "Không thể xóa vai trò đang được sử dụng."));

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<object>.Fail("DELETE_ROLE_FAILED", "Xóa vai trò thất bại."));

        await _auditLogService.LogAdminActionAsync(adminId , _currentUser.FullName,
            EnumAdminActionType.RoleDeleted, EnumTargetEntityType.Role, roleId.ToString(), $"Admin đã xóa vai trò '{role.Name}'.");

        return NoContent(); 
    }
}
