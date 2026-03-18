using Hmz.Kolafi.Core.SharedKernel;
using Hmz.Kolafi.Core.UserAggregate.Events;

namespace Hmz.Kolafi.Core.UserAggregate;

/// <summary>
/// User aggregate root.
/// SubjectId stores the Logto `sub` claim (string) for external identity correlation.
/// </summary>
public class User : FullAuditableEntity<User, UserId>, IAggregateRoot
{
  public string SubjectId { get; private set; } = string.Empty;
  public string Email { get; private set; } = string.Empty;
  public string Name { get; private set; } = string.Empty;
  public string? Picture { get; private set; }
  public List<string> Roles { get; private set; } = [];
  public DateTimeOffset? LastSignedInAt { get; private set; }

  // EF Core parameterless constructor
  private User() { }

  public User(string subjectId, string email, string name, string? picture = null, List<string>? roles = null)
  {
    SubjectId = Guard.Against.NullOrWhiteSpace(subjectId, nameof(subjectId));
    Email = Guard.Against.NullOrWhiteSpace(email, nameof(email));
    Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));

    Picture = picture;
    Roles = roles ?? [];
  }

  public User UpdateProfile(string email, string name, string? picture, List<string>? roles)
  {
    Email = Guard.Against.NullOrWhiteSpace(email, nameof(email));
    Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
    Roles = roles ?? Roles;
    Picture = picture;

    return this;
  }

  public User RecordSignIn()
  {
    LastSignedInAt = DateTimeOffset.UtcNow;
    RegisterDomainEvent(new UserSignedInEvent(this));
    return this;
  }
}
