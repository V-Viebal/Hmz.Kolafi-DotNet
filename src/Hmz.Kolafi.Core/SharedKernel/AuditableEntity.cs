using Hmz.Kolafi.Core.Interfaces;

namespace Hmz.Kolafi.Core.SharedKernel;

/// <summary>
/// Base entity that adds auditing (CreatedAt/By, ModifiedAt/By).
/// Uses <see cref="long"/> as the default Id type.
/// </summary>
public abstract class AuditableEntity<TEntity> : AuditableEntity<TEntity, long>
  where TEntity : AuditableEntity<TEntity>
{
}

/// <summary>
/// Base entity that adds auditing (CreatedAt/By, ModifiedAt/By).
/// Use this when you need auditing but NOT soft-delete or optimistic locking.
///
/// For an entity that needs everything, use <see cref="FullAuditableEntity{TEntity, TId}"/>.
/// For an entity that needs no cross-cutting behavior, use Ardalis.SharedKernel.EntityBase directly.
/// </summary>
public abstract class AuditableEntity<TEntity, TId>
  : EntityBase<TEntity, TId>, IAuditable
  where TEntity : AuditableEntity<TEntity, TId>
  where TId : struct, IEquatable<TId>
{
  public DateTimeOffset CreatedAt { get; private set; }
  public string? CreatedBy { get; private set; }
  public DateTimeOffset ModifiedAt { get; private set; }
  public string? ModifiedBy { get; private set; }

  public void SetCreated(DateTimeOffset at, string? by)
  {
    CreatedAt = at;
    CreatedBy = by;
  }

  public void SetModified(DateTimeOffset at, string? by)
  {
    ModifiedAt = at;
    ModifiedBy = by;
  }
}
