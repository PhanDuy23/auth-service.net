using auth_service.DTOs.Requests;
using auth_service.DTOs.Responses;
using auth_service.Interfaces;
using auth_service.Models;
using auth_service.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace auth_service.Services;

public class AdminService : UserService, IAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminService(UserManager<ApplicationUser> userManager)
        : base(userManager)
    {
        _userManager = userManager;
    }

    // ── Get Users Paged ───────────────────────────────────────────────────────

    public async Task<ApiResponse<PagedResult<UserProfileDto>>> GetUsersPagedAsync(
        UserQueryDto query
    )
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(query.Page, 1);

        IQueryable<ApplicationUser> q = _userManager.Users;

        // Lọc IsActive
        if (query.IsActive.HasValue)
            q = q.Where(u => u.IsActive == query.IsActive.Value);

        // Tìm kiếm theo email hoặc FullName
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            q = q.Where(u =>
                u.Email!.ToLower().Contains(keyword)
                || (u.FullName != null && u.FullName.ToLower().Contains(keyword))
            );
        }

        // Lọc theo role — Identity không expose role trực tiếp trên IQueryable
        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(query.Role);
            var roleUserIds = usersInRole.Select(u => u.Id).ToHashSet();
            q = q.Where(u => roleUserIds.Contains(u.Id));
        }

        // Sort ổn định theo CreatedAt mới nhất
        q = q.OrderByDescending(u => u.CreatedAt);

        var totalCount = await q.CountAsync();
        var users = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var items = new List<UserProfileDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            items.Add(MapToDto(user, roles));
        }

        var result = new PagedResult<UserProfileDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };

        return ApiResponse<PagedResult<UserProfileDto>>.SuccessResponse(result);
    }

    // ── Get User By Id ────────────────────────────────────────────────────────

    public async Task<ApiResponse<UserProfileDto>> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ApiResponse<UserProfileDto>.ErrorResponse("Người dùng không tồn tại", 404);

        var roles = await _userManager.GetRolesAsync(user);
        return ApiResponse<UserProfileDto>.SuccessResponse(MapToDto(user, roles));
    }

    // ── Get Employees ─────────────────────────────────────────────────────────

    public async Task<ApiResponse<List<UserProfileDto>>> GetEmployeesAsync()
    {
        var employees = await _userManager.GetUsersInRoleAsync("Employee");

        var result = new List<UserProfileDto>();
        foreach (var user in employees.Where(u => u.IsActive))
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(MapToDto(user, roles));
        }

        return ApiResponse<List<UserProfileDto>>.SuccessResponse(result);
    }

    // ── Delete User (soft delete) ─────────────────────────────────────────────

    public async Task<ApiResponse<object>> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
            return ApiResponse<object>.ErrorResponse("Người dùng không tồn tại", 404);

        // Chỉ được xóa user có role Customer
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains("Customer"))
            return ApiResponse<object>.ErrorResponse(
                "Chỉ có thể xóa tài khoản có role Customer",
                403
            );

        user.IsActive = false;
        var identityResult = await _userManager.UpdateAsync(user);

        if (!identityResult.Succeeded)
        {
            var errors = identityResult.Errors.Select(e => e.Description).ToList();
            return ApiResponse<object>.ErrorResponse("Không thể xóa người dùng", 500, errors);
        }

        return ApiResponse<object>.SuccessResponse(null, "Xóa người dùng thành công");
    }

    // ── Lock / Unlock User ────────────────────────────────────────────────────

    public async Task<ApiResponse<object>> SetUserActiveAsync(string userId, bool isActive)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ApiResponse<object>.ErrorResponse("Người dùng không tồn tại", 404);

        if (user.IsActive == isActive)
        {
            var state = isActive ? "đang hoạt động" : "đã bị khóa";
            return ApiResponse<object>.ErrorResponse($"Tài khoản {state}", 400);
        }

        user.IsActive = isActive;
        var identityResult = await _userManager.UpdateAsync(user);

        if (!identityResult.Succeeded)
        {
            var errors = identityResult.Errors.Select(e => e.Description).ToList();
            return ApiResponse<object>.ErrorResponse(
                "Không thể cập nhật trạng thái tài khoản",
                500,
                errors
            );
        }

        var message = isActive ? "Mở khóa tài khoản thành công" : "Khóa tài khoản thành công";
        return ApiResponse<object>.SuccessResponse(null, message);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static UserProfileDto MapToDto(ApplicationUser user, IList<string> roles) =>
        new()
        {
            Id = user.Id,
            Email = user.Email!,
            FullName = user.FullName ?? string.Empty,
            Avatar = user.Avatar,
            DateOfBirth = user.DateOfBirth,
            Department = user.Department,
            Roles = roles,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive,
        };
}
