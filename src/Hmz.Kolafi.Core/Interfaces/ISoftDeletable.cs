namespace Hmz.Kolafi.Core.Interfaces;

/// <summary>
/// Interface for entities that support soft deletion.
/// Applied by the SoftDeleteInterceptor and global query filter in Infrastructure.
/// </summary>
public interface ISoftDeletable
{
  bool IsDeleted { get; }
  DateTimeOffset? DeletedAt { get; }
  string? DeletedBy { get; }

  void SoftDelete(DateTimeOffset at, string? by);
  void Restore();
}
