using System.ComponentModel.DataAnnotations;

namespace auth_service.DTOs.Requests;

public class ConfirmEmailDto
{
    [Required(ErrorMessage = "UserId là bắt buộc")]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Token là bắt buộc")]
    public string Token { get; set; } = string.Empty;
}
