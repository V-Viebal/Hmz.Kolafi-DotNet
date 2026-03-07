using Hmz.Kolafi.Core.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Hmz.Kolafi.Infrastructure.Data;

/// <summary>
/// Intercepts SaveChanges to convert hard deletes into soft deletes
/// for entities implementing ISoftDeletable.
/// </summary>
public class SoftDeleteInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
  public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
    DbContextEventData eventData,
    InterceptionResult<int> result,
    CancellationToken cancellationToken = default)
  {
    if (eventData.Context is not null)
      ConvertDeleteToSoftDelete(eventData.Context);

    return base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  public override InterceptionResult<int> SavingChanges(
    DbContextEventData eventData,
    InterceptionResult<int> result)
  {
    if (eventData.Context is not null)
      ConvertDeleteToSoftDelete(eventData.Context);

    return base.SavingChanges(eventData, result);
  }

  private void ConvertDeleteToSoftDelete(DbContext context)
  {
    var now = DateTimeOffset.UtcNow;
    var userId = currentUser.UserId;

    foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
    {
      if (entry.State == EntityState.Deleted)
      {
        entry.State = EntityState.Modified;
        entry.Entity.SoftDelete(now, userId);
      }
    }
  }
}
