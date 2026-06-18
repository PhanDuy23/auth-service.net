using auth_service.DTOs.Requests;
using auth_service.DTOs.Responses;
using auth_service.Interfaces;
using auth_service.Models;
using auth_service.Response;
using Microsoft.AspNetCore.Identity;

namespace auth_service.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // ── Get All Users ─────────────────────────────────────────────────────────

    public async Task<ApiResponse<List<UserProfileDto>>> GetUsersAsync()
    {
        var users = _userManager.Users.Where(u => u.IsActive).ToList();

        var result = new List<UserProfileDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(MapToDto(user, roles));
        }

        return ApiResponse<List<UserProfileDto>>.SuccessResponse(result);
    }

    // ── Get Profile ───────────────────────────────────────────────────────────

    public async Task<ApiResponse<UserProfileDto>> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
            return ApiResponse<UserProfileDto>.ErrorResponse("Người dùng không tồn tại", 404);

        var roles = await _userManager.GetRolesAsync(user);
        return ApiResponse<UserProfileDto>.SuccessResponse(MapToDto(user, roles));
    }

    // ── Update Profile ────────────────────────────────────────────────────────

    public async Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(
        string userId,
        UpdateProfileDto dto
    )
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
            return ApiResponse<UserProfileDto>.ErrorResponse("Người dùng không tồn tại", 404);

        // Chỉ cập nhật field nào được gửi lên (partial update)
        if (dto.FullName is not null)
            user.FullName = dto.FullName;
        if (dto.Avatar is not null)
            user.Avatar = dto.Avatar;
        if (dto.DateOfBirth.HasValue)
            user.DateOfBirth = dto.DateOfBirth;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return ApiResponse<UserProfileDto>.ErrorResponse(
                "Cập nhật thất bại",
                500,
                result.Errors.Select(e => e.Description).ToList()
            );

        var roles = await _userManager.GetRolesAsync(user);
        return ApiResponse<UserProfileDto>.SuccessResponse(
            MapToDto(user, roles),
            "Cập nhật profile thành công"
        );
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    protected static UserProfileDto MapToDto(ApplicationUser user, IList<string> roles) =>
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
