using System.ComponentModel.DataAnnotations;

namespace auth_service.DTOs.Requests;

public class RegisterDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Role hợp lệ: "User", "Employee" hoặc "Admin". Mặc định là "Customer" nếu không truyền.
    /// </summary>
    public string Role { get; set; } = "Customer";
}
