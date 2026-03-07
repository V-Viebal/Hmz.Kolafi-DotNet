namespace Hmz.Kolafi.UseCases.Users.SignIn;

/// <summary>
/// Command triggered by Logto webhook on user sign-in/sign-up.
/// Fetches user profile from Logto, upserts into DB, updates cache.
/// </summary>
public record SyncUserOnSignInCommand(string UserId) : ICommand<Result<UserProfileDTO>>;
