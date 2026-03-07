namespace Hmz.Kolafi.Core.Interfaces;

/// <summary>
/// Interface for entities that track creation and modification audit fields.
/// Applied by the AuditableInterceptor in Infrastructure.
/// </summary>
public interface IAuditable
{
  DateTimeOffset CreatedAt { get; }
  string? CreatedBy { get; }
  DateTimeOffset ModifiedAt { get; }
  string? ModifiedBy { get; }

  void SetCreated(DateTimeOffset at, string? by);
  void SetModified(DateTimeOffset at, string? by);
}
