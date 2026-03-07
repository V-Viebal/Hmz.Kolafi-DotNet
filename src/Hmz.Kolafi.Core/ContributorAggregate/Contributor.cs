using Hmz.Kolafi.Core.ContributorAggregate.Events;
using Hmz.Kolafi.Core.SharedKernel;

namespace Hmz.Kolafi.Core.ContributorAggregate;

/// <summary>
/// Contributor aggregate root.
/// Uses FullAuditableEntity for built-in auditing, soft-delete, and optimistic locking.
/// </summary>
public class Contributor(ContributorName name)
  : FullAuditableEntity<Contributor, ContributorId>, IAggregateRoot
{
  public ContributorName Name { get; private set; } = name;
  public ContributorStatus Status { get; private set; } = ContributorStatus.NotSet;
  public PhoneNumber? PhoneNumber { get; private set; }

  public Contributor UpdatePhoneNumber(PhoneNumber newPhoneNumber)
  {
    PhoneNumber = newPhoneNumber;
    return this;
  }

  public Contributor UpdateName(ContributorName newName)
  {
    if (Name == newName) return this;
    Name = newName;
    RegisterDomainEvent(new ContributorNameUpdatedEvent(this));
    return this;
  }
}
