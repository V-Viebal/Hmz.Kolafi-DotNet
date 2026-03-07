using Hmz.Kolafi.Core.Interfaces;
using Hmz.Kolafi.Core.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hmz.Kolafi.Infrastructure.Data.Config;

/// <summary>
/// Base configuration for common entity patterns.
/// Automatically configures auditing, soft-delete, and row version columns.
/// Entity-specific configurations should inherit from this and call base.Configure().
/// </summary>
/// <typeparam name="TEntity">The entity type being configured.</typeparam>
public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
  where TEntity : class
{
  public virtual void Configure(EntityTypeBuilder<TEntity> builder)
  {
    // Apply cross-cutting concern configurations in order
    ConfigureAuditable(builder);
    ConfigureSoftDeletable(builder);
    ConfigureRowVersion(builder);
    ConfigureEntitySpecific(builder);
  }

  /// <summary>
  /// Configures IAuditable properties: CreatedAt, CreatedBy, ModifiedAt, ModifiedBy.
  /// </summary>
  protected virtual void ConfigureAuditable(EntityTypeBuilder<TEntity> builder)
  {
    if (typeof(IAuditable).IsAssignableFrom(typeof(TEntity)))
    {
      builder.Property(nameof(IAuditable.CreatedAt))
        .IsRequired()
        .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");

      builder.Property(nameof(IAuditable.CreatedBy))
        .HasMaxLength(128);

      builder.Property(nameof(IAuditable.ModifiedAt))
        .IsRequired()
        .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");

      builder.Property(nameof(IAuditable.ModifiedBy))
        .HasMaxLength(128);

      // Index for audit queries
      builder.HasIndex(nameof(IAuditable.CreatedAt));
      builder.HasIndex(nameof(IAuditable.ModifiedAt));
    }
  }

  /// <summary>
  /// Configures ISoftDeletable properties: IsDeleted, DeletedAt, DeletedBy.
  /// Also applies a global query filter to exclude deleted records.
  /// </summary>
  protected virtual void ConfigureSoftDeletable(EntityTypeBuilder<TEntity> builder)
  {
    if (typeof(ISoftDeletable).IsAssignableFrom(typeof(TEntity)))
    {
      builder.Property(nameof(ISoftDeletable.IsDeleted))
        .IsRequired()
        .HasDefaultValue(false);

      builder.Property(nameof(ISoftDeletable.DeletedAt));

      builder.Property(nameof(ISoftDeletable.DeletedBy))
        .HasMaxLength(128);

      // Performance index: filter out soft-deleted records
      builder.HasIndex(nameof(ISoftDeletable.IsDeleted))
        .HasFilter($"\"{nameof(ISoftDeletable.IsDeleted)}\" = false")
        .HasDatabaseName($"ix_{typeof(TEntity).Name}_active");

      // Global query filter: automatically exclude soft-deleted records
      // This ensures all queries (except IgnoreQueryFilters()) return only active records
      var parameter = System.Linq.Expressions.Expression.Parameter(typeof(TEntity), "e");
      var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
      var notDeletedExpression = System.Linq.Expressions.Expression.Lambda(
        System.Linq.Expressions.Expression.Not(property),
        parameter);

      builder.HasQueryFilter((System.Linq.Expressions.Expression<System.Func<TEntity, bool>>)notDeletedExpression);
    }
  }

  /// <summary>
  /// Configures IHasRowVersion property for optimistic concurrency control.
  /// Uses PostgreSQL xmin system column via EF Core's IsRowVersion().
  /// </summary>
  protected virtual void ConfigureRowVersion(EntityTypeBuilder<TEntity> builder)
  {
    if (typeof(IHasRowVersion).IsAssignableFrom(typeof(TEntity)))
    {
      builder.Property(nameof(IHasRowVersion.RowVersion))
        .IsRowVersion();
    }
  }

  /// <summary>
  /// Override to apply entity-specific additional configuration.
  /// Call base.Configure() first, then add entity-specific settings.
  /// </summary>
  protected abstract void ConfigureEntitySpecific(EntityTypeBuilder<TEntity> builder);
}
