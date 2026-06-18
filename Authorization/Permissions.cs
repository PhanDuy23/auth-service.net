namespace auth_service.Authorization;

/// <summary>
/// Định nghĩa tất cả permission constants trong hệ thống.
/// Dùng làm claim value khi tạo JWT và đăng ký policy.
/// </summary>
public static class Permissions
{
    public const string UsersRead = "users.read";
    public const string UsersDelete = "users.delete";
    public const string ProfileEdit = "profile.edit";
}
