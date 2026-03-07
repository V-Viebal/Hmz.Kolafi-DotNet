namespace Hmz.Kolafi.Core.FeatureFlags;

/// <summary>
/// Centralized, type-safe module feature flag names.
/// Each constant maps to a key under "FeatureManagement" in appsettings.json.
/// When adding a new module, add a new constant here and configure it in appsettings.json.
/// </summary>
public static class FeatureFlags
{
  public const string ContributorsModule = "ContributorsModule";
  public const string UsersModule = "UsersModule";
}
