using Hmz.Kolafi.Core.UserAggregate.Events;

namespace Hmz.Kolafi.Core.UserAggregate.Handlers;

/// <summary>
/// Handles UserSignedInEvent — logs the sign-in for audit purposes.
/// Extend this handler with additional side effects as needed (e.g., analytics, notifications).
/// </summary>
public class UserSignedInHandler(ILogger<UserSignedInHandler> logger)
  : INotificationHandler<UserSignedInEvent>
{
  public ValueTask Handle(UserSignedInEvent notification, CancellationToken ct)
  {
    logger.LogInformation(
      "User {UserId} ({Email}) signed in at {SignedInAt}",
      notification.User.Id,
      notification.User.Email,
      notification.User.LastSignedInAt);

    return ValueTask.CompletedTask;
  }
}
