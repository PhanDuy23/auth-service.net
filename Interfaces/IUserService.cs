using auth_service.DTOs.Requests;
using auth_service.DTOs.Responses;
using auth_service.Response;

namespace auth_service.Interfaces;

public interface IUserService
{
    Task<ApiResponse<UserProfileDto>> GetProfileAsync(string userId);
    Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(string userId, UpdateProfileDto dto);
    Task<ApiResponse<List<UserProfileDto>>> GetUsersAsync();
}
