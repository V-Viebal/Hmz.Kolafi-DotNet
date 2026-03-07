using Hmz.Kolafi.Core.UserAggregate;

namespace Hmz.Kolafi.Core.Interfaces;

/// <summary>
/// HybridCache-backed service for retrieving user profiles.
/// Cache key: "user:{userId}"
/// </summary>
public interface ICachedUserProfileService
{
  /// <summary>
  /// Gets user profile from cache, falling back to the database.
  /// </summary>
  Task<UserProfileDTO?> GetProfileAsync(string userId, CancellationToken ct = default);

  /// <summary>
  /// Invalidates the cached profile for a user (call after upsert).
  /// </summary>
  Task InvalidateAsync(string userId, CancellationToken ct = default);
}
