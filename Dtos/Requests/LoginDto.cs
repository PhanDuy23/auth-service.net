using System.ComponentModel.DataAnnotations;

namespace auth_service.DTOs.Requests;

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// true  → persistent cookie (có Expires = RefreshTokenExpiryHours)
    /// false → session cookie (không có Expires, mất khi đóng browser)
    /// </summary>
    public bool RememberMe { get; set; } = false;
}
