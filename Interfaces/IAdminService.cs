using auth_service.DTOs.Requests;
using auth_service.DTOs.Responses;
using auth_service.Response;

namespace auth_service.Interfaces;

public interface IAdminService : IUserService
{
    Task<ApiResponse<PagedResult<UserProfileDto>>> GetUsersPagedAsync(UserQueryDto query);
    Task<ApiResponse<UserProfileDto>> GetUserByIdAsync(string userId);
    Task<ApiResponse<List<UserProfileDto>>> GetEmployeesAsync();
    Task<ApiResponse<object>> DeleteUserAsync(string userId);
    Task<ApiResponse<object>> SetUserActiveAsync(string userId, bool isActive);
}
