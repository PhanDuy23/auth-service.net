using System.Security.Cryptography;
using auth_service.Data;
using auth_service.DTOs.Responses;
using auth_service.Interfaces;
using auth_service.Models;
using Microsoft.EntityFrameworkCore;

namespace auth_service.Services;

/// <summary>
/// Service quản lý Recovery Codes — backup codes dùng khi mất quyền truy cập authenticator.
/// </summary>
public class RecoveryCodeService : IRecoveryCodeService
{
    private readonly ApplicationDbContext _db;

    public RecoveryCodeService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<RecoveryCodesDto> GenerateCodesAsync(string userId, int count = 8)
    {
        if (count < 6 || count > 20)
            throw new ArgumentException(
                "Recovery code count must be between 6 and 20",
                nameof(count)
            );

        // Xóa tất cả codes cũ trước khi sinh mới (regenerate)
        await RevokeAllAsync(userId);

        var rawCodes = new List<string>();

        for (var i = 0; i < count; i++)
        {
            // Tạo raw code 8 ký tự hex (4 bytes random) → định dạng XXXX-XXXX
            var bytes = RandomNumberGenerator.GetBytes(4);
            var hex = Convert.ToHexString(bytes).ToLowerInvariant();
            var formatted = $"{hex[..4]}-{hex[4..]}"; // e.g. "a3f2-b9c1"

            var codeHash = HashCode(formatted);

            _db.RecoveryCodes.Add(
                new RecoveryCode
                {
                    UserId = userId,
                    CodeHash = codeHash,
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );

            rawCodes.Add(formatted);
        }

        await _db.SaveChangesAsync();

        return new RecoveryCodesDto { Codes = rawCodes };
    }

    public async Task<bool> VerifyAndMarkUsedAsync(string userId, string rawCode)
    {
        // Chuẩn hóa: bỏ dấu gạch ngang, lowercase
        var normalized = rawCode.Replace("-", "").Trim().ToLowerInvariant();

        // Tính hash
        var codeHash = HashCode(normalized);

        var code = await _db
            .RecoveryCodes.Where(c =>
                c.UserId == userId && c.CodeHash == codeHash && c.UsedAt == null
            )
            .FirstOrDefaultAsync();

        if (code == null)
            return false;

        // Đánh dấu đã dùng
        code.UsedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task RevokeAllAsync(string userId)
    {
        var codes = await _db.RecoveryCodes.Where(c => c.UserId == userId).ToListAsync();

        if (codes.Any())
        {
            _db.RecoveryCodes.RemoveRange(codes);
            await _db.SaveChangesAsync();
        }
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    /// <summary>
    /// SHA-256 hash của recovery code — không bao giờ lưu raw code vào DB.
    /// Chuẩn hóa trước khi hash (lowercase, no dash).
    /// </summary>
    private static string HashCode(string rawCode)
    {
        // Chuẩn hóa: bỏ dash, lowercase
        var normalized = rawCode.Replace("-", "").Trim().ToLowerInvariant();
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
