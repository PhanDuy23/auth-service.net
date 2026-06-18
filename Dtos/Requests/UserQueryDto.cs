namespace auth_service.DTOs.Requests;

/// <summary>
/// Query parameters cho endpoint liệt kê / tìm kiếm user (Admin)
/// </summary>
public class UserQueryDto
{
    /// <summary>Tìm theo email hoặc họ tên (case-insensitive, partial match)</summary>
    public string? Search { get; set; }

    /// <summary>Lọc theo role: Customer | Employee | Admin</summary>
    public string? Role { get; set; }

    /// <summary>Lọc theo trạng thái: true = active, false = inactive, null = tất cả</summary>
    public bool? IsActive { get; set; }

    /// <summary>Trang hiện tại, bắt đầu từ 1</summary>
    public int Page { get; set; } = 1;

    /// <summary>Số bản ghi mỗi trang, tối đa 100</summary>
    public int PageSize { get; set; } = 10;
}
