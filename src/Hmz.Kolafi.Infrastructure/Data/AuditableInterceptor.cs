using Hmz.Kolafi.Core.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Hmz.Kolafi.Infrastructure.Data;

/// <summary>
/// Intercepts SaveChanges to automatically populate audit fields (CreatedAt/By, ModifiedAt/By)
/// on entities implementing IAuditable.
/// Uses ICurrentUser (from Core) to resolve the current actor — no ASP.NET dependency here.
/// </summary>
public class AuditableInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
  public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
    DbContextEventData eventData,
    InterceptionResult<int> result,
    CancellationToken cancellationToken = default)
  {
    if (eventData.Context is not null)
      ApplyAuditFields(eventData.Context);

    return base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  public override InterceptionResult<int> SavingChanges(
    DbContextEventData eventData,
    InterceptionResult<int> result)
  {
    if (eventData.Context is not null)
      ApplyAuditFields(eventData.Context);

    return base.SavingChanges(eventData, result);
  }

  private void ApplyAuditFields(DbContext context)
  {
    var now = DateTimeOffset.UtcNow;
    var userId = currentUser.UserId;

    foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
    {
      if (entry.State == EntityState.Added)
      {
        entry.Entity.SetCreated(now, userId);
        entry.Entity.SetModified(now, userId);
      }
      else if (entry.State == EntityState.Modified)
      {
        entry.Entity.SetModified(now, userId);
      }
    }
  }
}
