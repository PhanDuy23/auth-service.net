using System.ComponentModel.DataAnnotations;

namespace auth_service.DTOs.Requests;

public class RefreshTokenDto
{
    [Required(ErrorMessage = "Refresh token không được để trống")]
    public string RefreshToken { get; set; } = string.Empty;
}
