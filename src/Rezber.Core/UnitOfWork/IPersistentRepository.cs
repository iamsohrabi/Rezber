using Rezber.Core.Domain;

namespace Rezber.Core.UnitOfWork;

/// <summary>
/// When we want to manipulate information in the database
/// </summary>
/// <typeparam name="T">An entity that exists in the database</typeparam>
/// <typeparam name="TID">The type of key stored in the database</typeparam>
public interface IPersistentRepository<T, TID> where T : AuditAggregate<TID>
{
    /// <summary>
    /// Creates a new entity of type T asynchronously in the database.
    /// </summary>
    /// <param name="entity">entity The entity object to create in the database.</param>
    /// <returns>Task</returns>
    Task CreateAsync(T entity);

    /// <summary>
    /// Updates an existing entity of type T asynchronously in the database.
    /// </summary>
    /// <param name="entity">entity The entity object containing updated values to persist in the database.</param>
    /// <returns>Task</returns>
    Task UpdateAsync(T entity);

    /// <summary>
    /// Removes an entity with the given ID asynchronously from the database.
    /// </summary>
    /// <param name="id">Id of key in database</param>
    /// <returns>Task</returns>
    Task RemoveAsync(TID id);
}