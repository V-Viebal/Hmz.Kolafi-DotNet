using Hmz.Kolafi.Core.Interfaces;
using Hmz.Kolafi.Core.UserAggregate;
using Hmz.Kolafi.Core.UserAggregate.Specifications;
using Microsoft.Extensions.Caching.Hybrid;

namespace Hmz.Kolafi.Infrastructure.Identity;

/// <summary>
/// HybridCache-backed user profile service.
/// Cache key pattern: "user-profile:{userId}"
/// </summary>
public class CachedUserProfileService(
  HybridCache _cache,
  IReadRepository<User> _repository,
  ILogger<CachedUserProfileService> _logger) : ICachedUserProfileService
{
  private static string CacheKey(string userId) => $"user-profile:{userId}";

  public async Task<UserProfileDTO?> GetProfileAsync(string userId, CancellationToken ct = default)
  {
    var profile = await _cache.GetOrCreateAsync(
      CacheKey(userId),
      async cancel =>
      {
        _logger.LogInformation("Cache miss for user {UserId}, loading from database", userId);
        var spec = new UserBySubjectSpec(userId);
        var user = await _repository.SingleOrDefaultAsync(spec, cancel);

        if (user is null) return null;

        return new UserProfileDTO(
          user.Id.Value,
          user.SubjectId,
          user.Email,
          user.Name,
          user.Picture,
          user.Roles,
          user.LastSignedInAt);
      },
      options: new HybridCacheEntryOptions
      {
        Expiration = TimeSpan.FromMinutes(15),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
      },
      cancellationToken: ct);

    return profile;
  }

  public async Task InvalidateAsync(string userId, CancellationToken ct = default)
  {
    _logger.LogInformation("Invalidating cache for user {UserId}", userId);
    await _cache.RemoveAsync(CacheKey(userId), ct);
  }
}
