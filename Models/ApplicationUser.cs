using Microsoft.AspNetCore.Identity;

namespace auth_service.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? Avatar { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Department { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// TOTP secret key (Base32) — chỉ set khi user đã xác nhận setup 2FA
    /// TwoFactorEnabled (từ IdentityUser) là flag chính để kiểm tra 2FA có bật không
    /// </summary>
    public string? TwoFactorSecretKey { get; set; }
}
