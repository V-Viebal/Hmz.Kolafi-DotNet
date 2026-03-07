namespace Hmz.Kolafi.Core.Interfaces;

/// <summary>
/// Interface for entities that use optimistic concurrency via a row version token.
/// Configured in EF Core as a concurrency token.
/// </summary>
public interface IHasRowVersion
{
  uint RowVersion { get; }
}
