namespace SampleDotnet.RepositoryFactory;

/// <summary>
/// Extension methods for <see cref="DbContext"/> to add additional functionality.
/// </summary>
internal static class DbContextExtensions
{
    /// <summary>
    /// Rolls back all the changes made in the tracked entities of the <see cref="DbContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/> instance on which to perform the rollback.</param>
    /// <param name="overrideDetectChanges">Indicates whether to force the detection of changes even if AutoDetectChanges is disabled.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal static async Task RollbackChangesAsync(this DbContext context, bool overrideDetectChanges = false, CancellationToken cancellationToken = default)
    {
        // Detect changes manually if override is enabled and AutoDetectChanges is off.
        if (overrideDetectChanges && !context.ChangeTracker.AutoDetectChangesEnabled)
            context.ChangeTracker.DetectChanges();

        // If there are changes to be rolled back.
        if (context.ChangeTracker.HasChanges())
        {
            // Iterate over all tracked entries and rollback state changes.
            foreach (var entry in context.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged && e.State != EntityState.Detached))
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        // Revert modified properties to their original values.
                        foreach (string propertyName in entry.OriginalValues.Properties.Select(f => f.Name))
                        {
                            entry.Property(propertyName).CurrentValue = entry.Property(propertyName).OriginalValue;
                        }
                        break;

                    case EntityState.Deleted:
                        // Change the state to Modified and then to Unchanged to cancel deletion.
                        entry.State = EntityState.Modified;
                        entry.State = EntityState.Unchanged;
                        break;

                    case EntityState.Added:
                        // Change the state to Deleted to remove newly added entities.
                        entry.State = EntityState.Deleted;
                        break;
                }
            }

            try
            {
                // Attempt to save the changes, effectively rolling back to the previous state.
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Ignored. This exception is expected if the SaveChangesAsync call fails due to concurrency conflicts.
                // These entities have already been validated with the previous SaveChangesAsync call, 
                // so this error occurs when attempting to save changes again.
            }
            catch (Exception)
            {
                // Re-throw any other exceptions.
                throw;
            }
        }
    }
}
