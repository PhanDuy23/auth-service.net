using auth_service.DTOs.Requests;
using auth_service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace auth_service.Controllers;

/// <summary>
/// Quản lý user — chỉ dành cho Admin
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// Liệt kê user có hỗ trợ tìm kiếm và phân trang
    /// </summary>
    /// <remarks>
    /// Query parameters:
    /// - search: tìm theo email hoặc họ tên (partial, case-insensitive)
    /// - role: lọc theo role (Customer | Employee | Admin)
    /// - isActive: true | false (bỏ trống = tất cả)
    /// - page: trang hiện tại, bắt đầu từ 1
    /// - pageSize: số bản ghi mỗi trang, tối đa 100 (mặc định 10)
    /// </remarks>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] UserQueryDto query)
    {
        var result = await _adminService.GetUsersPagedAsync(query);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Lấy thông tin chi tiết một user theo ID
    /// </summary>
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserById(string id)
    {
        var result = await _adminService.GetUserByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    // /// <summary>
    // /// Xóa user theo ID (soft delete) — chỉ áp dụng cho Customer
    // /// </summary>
    // [HttpDelete("users/{id}")]
    // public async Task<IActionResult> DeleteUser(string id)
    // {
    //     var result = await _adminService.DeleteUserAsync(id);
    //     return StatusCode(result.StatusCode, result);
    // }

    /// <summary>
    /// Lấy danh sách tất cả Employee
    /// </summary>
    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees()
    {
        var result = await _adminService.GetEmployeesAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Khóa tài khoản user
    /// </summary>
    [HttpPatch("users/{id}/lock")]
    public async Task<IActionResult> LockUser(string id)
    {
        var result = await _adminService.SetUserActiveAsync(id, false);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Mở khóa tài khoản user
    /// </summary>
    [HttpPatch("users/{id}/unlock")]
    public async Task<IActionResult> UnlockUser(string id)
    {
        var result = await _adminService.SetUserActiveAsync(id, true);
        return StatusCode(result.StatusCode, result);
    }
}
