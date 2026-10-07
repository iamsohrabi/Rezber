using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Rezber.Core.Domain;

namespace Rezber.Core.UnitOfWork;

/// <summary>
/// When we want to get only the information from the database
/// </summary>
/// <typeparam name="T">An entity that exists in the database</typeparam>
/// <typeparam name="TID">The type of key stored in the database</typeparam>
public interface IReadOnlyRepository<T, TID> where T : AuditAggregate<TID>
{
    /// <summary>
    /// Retrieves all entities asynchronously.
    /// </summary>
    Task<IReadOnlyCollection<T>> GetAllAsync();

    ///<summary>
    /// Retrieves all entities that match the given expression asynchronously.
    /// </summary>
    /// <param name="expression">Linq query</param>
    /// <returns>A real only collection of T class</returns>
    Task<IReadOnlyCollection<T>> GetAllAsync(Expression<Func<T, bool>> expression);

    /// <summary>
    /// Retrieves all entities that match the given filter definition asynchronously.
    /// </summary>
    /// <param name="filterBuilder">a FilterDefinition query for search</param>
    /// <returns>A real only collection of T class</returns>
    Task<IReadOnlyCollection<T>> GetAllAsync(FilterDefinition<T> filterBuilder);

    
    /// <summary>
    /// Retrieves all entities that match the given filter definition asynchronously.
    /// </summary>
    /// <param name="filterBuilder">a FilterDefinition query for search</param>
    /// <param name="sort">a SortDefinition query for sort data</param>
    /// <returns>A real only collection of T class</returns>
    Task<IReadOnlyCollection<T>> GetAllAsync(FilterDefinition<T> filter, SortDefinition<T> sort);

    /// <summary>
    /// Retrieves an entity by ID asynchronously.
    /// </summary>
    /// <param name="id">Key of data in database</param>
    /// <returns>An object of T class</returns>
    Task<T> GetAsync(TID id);

    /// <summary>
    /// Retrieves an entity asynchronously that matches the given expression.
    /// </summary>
    /// <param name="filter">FilterDefinition MongoDB</param>
    /// <returns>An object of T class</returns>
    Task<T?> GetAsync(FilterDefinition<T> filter);

    /// <summary>
    /// Retrieves an entity asynchronously that matches the given expression.
    /// </summary>
    /// <param name="expression">Linq query</param>
    /// <returns>An object of T class</returns>
    Task<T> GetAsync(Expression<Func<T, bool>> expression);

    Task<bool> AnyAsync(Expression<Func<T, bool>> expression);

    Task<bool> AnyAsync(FilterDefinition<T> filter);

    Task<bool> AnyAsync(TID id);

    /// <summary>
    /// Counts all non-deleted entities matching the specified filter expression.
    /// </summary>
    Task<long> CountAsync(Expression<Func<T, bool>> expression);

    /// <summary>
    /// Counts all non-deleted entities matching the specified FilterDefinition.
    /// </summary>
    Task<long> CountAsync(FilterDefinition<T> filter);

    /// <summary>
    /// Counts all non-deleted entities.
    /// </summary>
    Task<long> CountAsync();


    /// <summary>
    /// Gets a paged list of entities based on filter and sort definitions.
    /// </summary>
    Task<PagedResult<T>> GetPagedAsync(
        FilterDefinition<T> filter,
        SortDefinition<T> sort,
        int page,
        int pageSize);

    /// <summary>
    /// Gets a paged list of entities based on expression filter and sort definition.
    /// </summary>
    Task<PagedResult<T>> GetPagedAsync(
        Expression<Func<T, bool>> expression,
        SortDefinition<T> sort,
        int page,
        int pageSize);

    /// <summary>
    /// Executes a group aggregation with a custom filter, returning the first result as BsonDocument.
    /// </summary>   
    Task<BsonDocument?> GroupAsync(FilterDefinition<T> filter, BsonDocument groupExpression);

    /// <summary>
    /// Executes a group aggregation on the collection (non-deleted entities only).
    /// </summary>
    /// <param name="groupExpression">The $group stage expression, e.g. new BsonDocument("_id", BsonNull.Value).Add("total", new BsonDocument("$sum", "$downloads"))</param>
    /// <returns>The first result of the group stage as a BsonDocument, or null if no result.</returns>
    Task<BsonDocument?> GroupAsync(BsonDocument groupExpression);
}