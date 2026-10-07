using System.Linq.Expressions;
using MongoDB.Driver;
using Rezber.Core.Domain;
using Rezber.Core.UnitOfWork;
using Rezber.Core.MongoDB.Models;
using MongoDB.Bson;

namespace Rezber.Core.MongoDB;

/// <summary>
/// MongoRepository implements a MongoDB repository for entities of type T.
/// It inherits from BaseRepository to get base CRUD operations.
/// The MongoRepository constructor takes a MongoDB database and collection name to operate on.
/// It implements IRepository to expose CRUD operations for the entity.
/// The entity T must implement IEntity with a Guid ID.
/// </summary>
public class MongoRepository<T> : BaseRepository<T, Guid>,
    IRepository<T> where T : AuditAggregate<Guid>
{
    public MongoRepository(IMongoDatabase database, string collectionName) :
        base(database, collectionName)
    {
    }
}

/// <summary>
/// MongoRepository implements a MongoDB repository for entities of type T.
/// It inherits from BaseRepository to get base CRUD operations.
/// The MongoRepository constructor takes a MongoDB database and collection name to operate on.  
/// </summary>
public class MongoRepository<T, TID> : BaseRepository<T, TID> where T : AuditAggregate<TID>
{
    public MongoRepository(IMongoDatabase database, string collectionName) :
        base(database, collectionName)
    {
    }
}

/// BaseRepository provides a base implementation of repository CRUD operations for entities of type T with ID type TID using MongoDB.
/// It implements IRepository and provides methods for creating, retrieving, updating, deleting entities.
/// It is abstract and meant to be inherited by concrete repository implementations that provide the database and collection.
/// The MongoCollection and FilterDefinitionBuilder are protected for use in child classes.
public abstract class BaseRepository<T, TID> : IRepository<T, TID>
    where T : AuditAggregate<TID>
{
    private readonly IMongoCollection<T> _mongoCollection;
    private readonly FilterDefinitionBuilder<T> _filterBuilder;

    protected IMongoCollection<T> MongoCollection => _mongoCollection;
    protected FilterDefinitionBuilder<T> FilterBuilder => _filterBuilder;

    public BaseRepository(IMongoDatabase database, string collectionName)
    {
        _mongoCollection = database.GetCollection<T>(collectionName);
        _filterBuilder = Builders<T>.Filter;
    }

    /// <summary>
    /// Checks if any entity exists matching the specified filter expression, ignoring soft-deleted entities (IsDeleted = false).
    /// </summary>
    public async Task<bool> AnyAsync(Expression<Func<T, bool>> expression)
    {
        var filter = _filterBuilder.And(expression, _filterBuilder.Eq(e => e.IsDeleted, false));
        return await _mongoCollection.Find(filter).AnyAsync();
    }

    /// <summary>
    /// Checks if any entity exists matching the specified FilterDefinition, ignoring soft-deleted entities (IsDeleted = false).
    /// </summary>
    public async Task<bool> AnyAsync(FilterDefinition<T> filter)
    {
        var combinedFilter = _filterBuilder.And(filter, _filterBuilder.Eq(e => e.IsDeleted, false));
        return await _mongoCollection.Find(combinedFilter).AnyAsync();
    }

    /// <summary>
    /// Checks if any entity exists with the specified ID and not soft-deleted.
    /// </summary>
    public async Task<bool> AnyAsync(TID id)
    {
        var filter = _filterBuilder.And(
            _filterBuilder.Eq(e => e.Id, id),
            _filterBuilder.Eq(e => e.IsDeleted, false));
        return await _mongoCollection.Find(filter).AnyAsync();
    }

    public async Task CreateAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await _mongoCollection.InsertOneAsync(entity);
    }

    public void CreateIndexes(IndexConfig indexConfig)
    {
        var indexOptions = new CreateIndexOptions
        {
            Unique = indexConfig.IsUnique,
            Name = indexConfig.Name
        };

        IndexKeysDefinition<T>? indexKeys = null;

        // Text index.
        if (indexConfig.IsText)
        {
            var textFields = indexConfig.Fields.Where(f => f.IsText).ToList();
            if (textFields.Any())
            {
                indexKeys = Builders<T>.IndexKeys.Text(textFields.First().FieldName);
                foreach (var field in textFields.Skip(1))
                {
                    indexKeys = Builders<T>.IndexKeys.Combine(
                        indexKeys,
                        Builders<T>.IndexKeys.Text(field.FieldName)
                    );
                }
            }
        }
        // Regular index.
        else
        {
            foreach (var field in indexConfig.Fields)
            {
                var key = field.Direction == IndexDirection.Ascending
                    ? Builders<T>.IndexKeys.Ascending(field.FieldName)
                    : Builders<T>.IndexKeys.Descending(field.FieldName);

                indexKeys = indexKeys == null ? key : Builders<T>.IndexKeys.Combine(indexKeys, key);
            }

            // Backward compatibility.
            if (!indexConfig.Fields.Any() && indexConfig.Ascending.Any())
            {
                foreach (var field in indexConfig.Ascending)
                {
                    var key = Builders<T>.IndexKeys.Ascending(field);
                    indexKeys = indexKeys == null ? key : Builders<T>.IndexKeys.Combine(indexKeys, key);
                }
            }
        }

        if (indexKeys != null)
        {
            try
            {
                _mongoCollection.Indexes.CreateOne(new CreateIndexModel<T>(indexKeys, indexOptions));
            }
            catch (MongoCommandException ex) when (ex.Code == 85 || ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                // Ignore duplicate index creation during startup retries.
            }
        }
    }

    public async Task<IReadOnlyCollection<T>> GetAllAsync()
    {
        var finds = await _mongoCollection.Find(_filterBuilder.Eq(e => e.IsDeleted, false)).ToListAsync();

        if (finds is null)
            return new List<T>();

        return finds;
    }

    public async Task<IReadOnlyCollection<T>> GetAllAsync(FilterDefinition<T> filter, SortDefinition<T> sort)
    {
        var combinedFilter = _filterBuilder.And(filter, _filterBuilder.Eq(e => e.IsDeleted, false));
        
        var items = await _mongoCollection
            .Find(combinedFilter)
            .Sort(sort)
            .ToListAsync();
        
        return items;
    }

    public async Task<PagedResult<T>> GetPagedAsync(FilterDefinition<T> filter,
                                                    SortDefinition<T> sort,
                                                    int page,
                                                    int pageSize)
    {
        var combinedFilter = _filterBuilder.And(filter, _filterBuilder.Eq(e => e.IsDeleted, false));

        var totalCount = await _mongoCollection.CountDocumentsAsync(combinedFilter);

        var items = await _mongoCollection
            .Find(combinedFilter)
            .Sort(sort)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyCollection<T>> GetAllAsync(Expression<Func<T, bool>> expression)
    {
        var finds = await _mongoCollection.Find(
            _filterBuilder.And(expression, _filterBuilder.Eq(e => e.IsDeleted, false)))
            .ToListAsync();

        if (!finds.Any())
            return new List<T>();

        return finds;
    }
    public async Task<IReadOnlyCollection<T>> GetAllAsync(FilterDefinition<T> filterBuilder)
    {
        var finds = await _mongoCollection.Find(
            _filterBuilder.And(filterBuilder, _filterBuilder.Eq(e => e.IsDeleted, false))
            ).ToListAsync();

        if (finds is null)
            return new List<T>();

        return finds;
    }

    public async Task<T> GetAsync(TID id)
    {
        FilterDefinition<T> filter = _filterBuilder.And(
            _filterBuilder.Eq(e => e.Id, id),
            _filterBuilder.Eq(e => e.IsDeleted, false));
        return await _mongoCollection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<T?> GetAsync(FilterDefinition<T> filter)
        => await _mongoCollection.Find(filter).FirstOrDefaultAsync();


    public async Task<T> GetAsync(Expression<Func<T, bool>> expression)
        => await _mongoCollection.Find(
            _filterBuilder.And(expression, _filterBuilder.Eq(e => e.IsDeleted, false))
            ).FirstOrDefaultAsync();

    public async Task RemoveAsync(TID id)
    {
        FilterDefinition<T> filter =
            _filterBuilder.Eq(e => e.Id, id);

        await _mongoCollection.DeleteOneAsync(filter);
    }

    public async Task RemoveAsync(T entity)
        => await UpdateAsync(entity);

    public async Task UpdateAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        FilterDefinition<T> filter =
            _filterBuilder.Eq(e => e.Id, entity.Id);

        await _mongoCollection.ReplaceOneAsync(filter, entity);
    }

    /// <summary>
    /// Counts all non-deleted entities matching the specified filter expression.
    /// </summary>
    public async Task<long> CountAsync(Expression<Func<T, bool>> expression)
    {
        var filter = _filterBuilder.And(expression, _filterBuilder.Eq(e => e.IsDeleted, false));
        return await _mongoCollection.CountDocumentsAsync(filter);
    }

    /// <summary>
    /// Counts all non-deleted entities matching the specified FilterDefinition.
    /// </summary>
    public async Task<long> CountAsync(FilterDefinition<T> filter)
    {
        var combinedFilter = _filterBuilder.And(filter, _filterBuilder.Eq(e => e.IsDeleted, false));
        return await _mongoCollection.CountDocumentsAsync(combinedFilter);
    }

    /// <summary>
    /// Counts all non-deleted entities.
    /// </summary>
    public async Task<long> CountAsync()
    {
        var filter = _filterBuilder.Eq(e => e.IsDeleted, false);
        return await _mongoCollection.CountDocumentsAsync(filter);
    }

    /// <summary>
    /// Gets a paged list of entities based on expression filter and sort definition.
    /// </summary>
    public async Task<PagedResult<T>> GetPagedAsync(Expression<Func<T, bool>> expression,
                                                    SortDefinition<T> sort,
                                                    int page,
                                                    int pageSize)
    {
        var combinedFilter = _filterBuilder.And(expression, _filterBuilder.Eq(e => e.IsDeleted, false));

        var totalCount = await _mongoCollection.CountDocumentsAsync(combinedFilter);

        var items = await _mongoCollection
            .Find(combinedFilter)
            .Sort(sort)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Executes a group aggregation on the collection (non-deleted entities only).
    /// </summary>
    /// <param name="groupExpression">The $group stage expression, e.g. new BsonDocument("_id", BsonNull.Value).Add("total", new BsonDocument("$sum", "$downloads"))</param>
    /// <returns>The first result of the group stage as a BsonDocument, or null if no result.</returns>
    public async Task<BsonDocument?> GroupAsync(BsonDocument groupExpression)
    {
        var pipeline = new EmptyPipelineDefinition<T>()
            .Match(_filterBuilder.Eq(e => e.IsDeleted, false))
            .Group(groupExpression);

        return await _mongoCollection
            .Aggregate(pipeline)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Executes a group aggregation with a custom filter, returning the first result as BsonDocument.
    /// </summary>
    public async Task<BsonDocument?> GroupAsync(FilterDefinition<T> filter, BsonDocument groupExpression)
    {
        var combinedFilter = _filterBuilder.And(filter, _filterBuilder.Eq(e => e.IsDeleted, false));

        var pipeline = new EmptyPipelineDefinition<T>()
            .Match(combinedFilter)
            .Group(groupExpression);

        return await _mongoCollection
            .Aggregate(pipeline)
            .FirstOrDefaultAsync();
    }

}
