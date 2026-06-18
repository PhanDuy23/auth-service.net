namespace auth_service.Interfaces;

public interface ICookieService
{
    void SetRefreshTokenCookie(string token, DateTimeOffset expiresAt, bool persistent);
    void ClearRefreshTokenCookie();

    void SetPendingTokenCookie(string token);
    void ClearPendingTokenCookie();

    string? GetRefreshToken();
    string? GetPendingToken();
}
