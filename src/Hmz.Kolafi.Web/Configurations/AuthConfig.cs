using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Hmz.Kolafi.Infrastructure.Identity;

namespace Hmz.Kolafi.Web.Configurations;

public static class AuthConfig
{
  public static IServiceCollection AddAuthenticationConfigs(
    this IServiceCollection services,
    IConfiguration configuration,
    Microsoft.Extensions.Logging.ILogger logger)
  {
    var logtoConfig = configuration.GetSection(LogtoConfiguration.SectionName).Get<LogtoConfiguration>();

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(options =>
      {
        options.Authority = logtoConfig?.Authority ?? "https://your-logto-domain.com/oidc";
        options.Audience = logtoConfig?.Audience ?? "https://your-api-resource";

        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          // Logto uses the "sub" claim for user identity
          NameClaimType = ClaimTypes.NameIdentifier,
          RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
          OnAuthenticationFailed = context =>
          {
            logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
          }
        };
      });

    services.AddAuthorization();

    logger.LogInformation("{Project} authentication configured", "Logto JWT Bearer");

    return services;
  }
}
