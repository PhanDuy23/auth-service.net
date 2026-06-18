using System.ComponentModel.DataAnnotations;

namespace auth_service.DTOs.Requests;

public class GitHubCallbackDto
{
    /// <summary>Authorization code nhận được từ GitHub OAuth callback.</summary>
    [Required(ErrorMessage = "Code không được để trống.")]
    public string Code { get; set; } = string.Empty;
}
