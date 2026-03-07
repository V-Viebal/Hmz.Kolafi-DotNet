using Hmz.Kolafi.Core.Interfaces;

namespace Hmz.Kolafi.Infrastructure.Identity;

/// <summary>
/// Fetches user profile from Logto Management API.
/// Currently uses dummy data — replace with real HTTP calls when ready.
/// </summary>
#pragma warning disable CS9113 // Parameter is unread — will be used when real Logto API calls replace the dummy impl
public class LogtoUserService(
  IHttpClientFactory _httpClientFactory,
  IOptions<LogtoConfiguration> _options,
  ILogger<LogtoUserService> _logger) : ILogtoUserService
#pragma warning restore CS9113
{
  public async Task<LogtoUserProfile?> GetUserProfileAsync(string userId, CancellationToken ct = default)
  {
    _logger.LogInformation("Fetching user profile from Logto for {UserId}", userId);

    // TODO: Replace with real Logto Management API call when credentials are configured.
    // Real implementation would be:
    //
    // var client = _httpClientFactory.CreateClient("LogtoManagement");
    // client.DefaultRequestHeaders.Authorization =
    //     new AuthenticationHeaderValue("Bearer", _options.Value.ManagementApiToken);
    // var response = await client.GetAsync($"/api/users/{userId}", ct);
    // if (!response.IsSuccessStatusCode) return null;
    // var logtoUser = await response.Content.ReadFromJsonAsync<LogtoApiUser>(ct);
    // return new LogtoUserProfile(logtoUser.Id, logtoUser.PrimaryEmail, logtoUser.Name, logtoUser.Avatar, logtoUser.Roles);

    // Dummy implementation — returns a fake profile based on userId
    await Task.CompletedTask;

    return new LogtoUserProfile(
      Id: userId,
      Email: $"{userId}@example.com",
      Name: $"User {userId[..Math.Min(8, userId.Length)]}",
      Picture: null,
      Roles: ["user"]);
  }
}
