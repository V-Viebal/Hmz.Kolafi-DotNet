namespace Hmz.Kolafi.Infrastructure.Identity;

/// <summary>
/// Configuration options for Logto identity provider.
/// Bound from appsettings.json "Logto" section.
/// </summary>
public class LogtoConfiguration
{
  public const string SectionName = "Logto";

  /// <summary>Logto OIDC authority (e.g. https://your-logto-domain.com/oidc)</summary>
  public string Authority { get; set; } = string.Empty;

  /// <summary>API resource indicator / audience for JWT validation</summary>
  public string Audience { get; set; } = string.Empty;

  /// <summary>Logto Management API base URL (e.g. https://your-logto-domain.com)</summary>
  public string ManagementApiEndpoint { get; set; } = string.Empty;

  /// <summary>Machine-to-machine access token for the Management API</summary>
  public string ManagementApiToken { get; set; } = string.Empty;

  /// <summary>Webhook signing secret for verifying Logto webhook payloads</summary>
  public string WebhookSigningSecret { get; set; } = string.Empty;
}
