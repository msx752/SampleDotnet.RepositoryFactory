internal static class DbContextExtensions
{
    internal static async Task RollbackChangesAsync(this DbContext context, bool overrideDetectChanges = false, CancellationToken cancellationToken = default)
    {
        if (overrideDetectChanges && !context.ChangeTracker.AutoDetectChangesEnabled)
            context.ChangeTracker.DetectChanges();

        if (context.ChangeTracker.HasChanges())
        {
            foreach (var entry in context.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged && e.State != EntityState.Detached))
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        foreach (string propertyName in entry.OriginalValues.Properties.Select(f => f.Name))
                        {
                            entry.Property(propertyName).CurrentValue = entry.Property(propertyName).OriginalValue;
                        }
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.State = EntityState.Unchanged;
                        break;

                    case EntityState.Added:
                        entry.State = EntityState.Deleted;
                        break;
                }
            }

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException e)
            {
                //ignored
                //entities already validated with previous SaveChangesAsync(); method,
                //this error comes from non-successful SaveChangesAsync(); which is thrown an exception.
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }

    internal static void SilentDbContextDispose(this DbContext dbContext, bool acceptAllChangesBeforeDisposing = false)
    {
        if (acceptAllChangesBeforeDisposing)
            dbContext.ChangeTracker.AcceptAllChanges();

        try
        {
            dbContext.Dispose();
        }
        catch (Exception e)
        {
        }
    }
}