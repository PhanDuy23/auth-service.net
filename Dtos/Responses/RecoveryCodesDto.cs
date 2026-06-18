namespace auth_service.DTOs.Responses;

/// <summary>
/// Trả về lần duy nhất khi bật 2FA — chứa các recovery codes dạng plain-text.
/// Người dùng phải lưu lại ngay, server không lưu raw codes.
/// </summary>
public class RecoveryCodesDto
{
    /// <summary>
    /// Danh sách raw recovery codes (plain-text, định dạng XXXX-XXXX).
    /// Chỉ hiển thị đúng một lần — không thể lấy lại sau này.
    /// </summary>
    public IReadOnlyList<string> Codes { get; set; } = [];

    /// <summary>
    /// Số lượng codes còn lại chưa dùng (dùng để hiển thị cảnh báo).
    /// </summary>
    public int RemainingCount => Codes.Count;
}
