using System.ComponentModel.DataAnnotations;

namespace auth_service.DTOs.Requests;

public class UpdateProfileDto
{
    [MaxLength(100)]
    public string? FullName { get; set; }

    [Url]
    [MaxLength(500)]
    public string? Avatar { get; set; }

    public DateOnly? DateOfBirth { get; set; }
}
