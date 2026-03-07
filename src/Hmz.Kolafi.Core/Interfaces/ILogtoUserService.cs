using Hmz.Kolafi.Core.UserAggregate;

namespace Hmz.Kolafi.Core.Interfaces;

/// <summary>
/// Fetches user profile from Logto Management API.
/// </summary>
public interface ILogtoUserService
{
  /// <summary>
  /// Gets user details from Logto by user ID (sub claim).
  /// Returns null if user is not found.
  /// </summary>
  Task<LogtoUserProfile?> GetUserProfileAsync(string userId, CancellationToken ct = default);
}

/// <summary>
/// Represents a user profile fetched from Logto Management API.
/// </summary>
public record LogtoUserProfile(
  string Id,
  string Email,
  string Name,
  string? Picture,
  List<string> Roles);
