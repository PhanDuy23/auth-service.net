public class AppSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string FrontendUrl { get; set; } = string.Empty;

    /// <summary>
    /// Tên ứng dụng hiển thị trong authenticator app (label của TOTP entry)
    /// </summary>
    public string AppName { get; set; } = "AuthService";
}
