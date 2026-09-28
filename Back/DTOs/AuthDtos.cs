using System.ComponentModel.DataAnnotations;

namespace EKR.API.DTOs;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record AuthResponse(
    string Token,
    DateTime ExpiresAt,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles);

public record UserDto(string Id, string Email, string FullName, IReadOnlyList<string> Roles);
