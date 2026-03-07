namespace Hmz.Kolafi.Core.UserAggregate.Events;

public sealed class UserSignedInEvent(User user) : DomainEventBase
{
  public User User { get; init; } = user;
}
