using Hmz.Kolafi.Core.Interfaces;

namespace Hmz.Kolafi.Core.SharedKernel;

/// <summary>
/// Base entity with full cross-cutting support (Auditing, Soft Delete, Row Version).
/// Uses <see cref="long"/> as the default Id type.
/// </summary>
public abstract class FullAuditableEntity<TEntity> : FullAuditableEntity<TEntity, long>
  where TEntity : FullAuditableEntity<TEntity>
{
}

/// <summary>
/// Base entity with full cross-cutting support:
/// auditing (CreatedAt/By, ModifiedAt/By), soft-delete (IsDeleted, DeletedAt/By),
/// and optimistic locking via PostgreSQL xmin (RowVersion).
///
/// This is the recommended base for most entities.
///
/// If you need only auditing: use <see cref="AuditableEntity{TEntity, TId}"/>.
/// If you need none of these: use Ardalis.SharedKernel.EntityBase directly.
/// </summary>
public abstract class FullAuditableEntity<TEntity, TId>
  : AuditableEntity<TEntity, TId>, ISoftDeletable, IHasRowVersion
  where TEntity : FullAuditableEntity<TEntity, TId>
  where TId : struct, IEquatable<TId>
{
  // ISoftDeletable
  public bool IsDeleted { get; private set; }
  public DateTimeOffset? DeletedAt { get; private set; }
  public string? DeletedBy { get; private set; }

  // IHasRowVersion — PostgreSQL xmin system column (uint is required by Npgsql)
  public uint RowVersion { get; private set; }

  public void SoftDelete(DateTimeOffset at, string? by)
  {
    IsDeleted = true;
    DeletedAt = at;
    DeletedBy = by;
  }

  public void Restore()
  {
    IsDeleted = false;
    DeletedAt = null;
    DeletedBy = null;
  }
}
