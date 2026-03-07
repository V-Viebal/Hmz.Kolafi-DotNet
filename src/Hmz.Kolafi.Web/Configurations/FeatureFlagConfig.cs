using Microsoft.FeatureManagement;

namespace Hmz.Kolafi.Web.Configurations;

public static class FeatureFlagConfig
{
  /// <summary>
  /// Registers Microsoft.FeatureManagement services.
  /// Feature flags are read from the "FeatureManagement" section in appsettings.json.
  /// </summary>
  public static IServiceCollection AddFeatureFlagConfig(
    this IServiceCollection services,
    IConfiguration configuration,
    Microsoft.Extensions.Logging.ILogger logger)
  {
    services.AddFeatureManagement(configuration.GetSection("FeatureManagement"));

    logger.LogInformation("{Config} registered", "FeatureManagement");

    return services;
  }
}
