using MongoDB.Driver;

namespace Rezber.Core.MongoDB.Models;

/// <summary>
/// Interface for a collection that defines its own indexes.
/// </summary>
public interface IIndexCreator
{
    string CollectionName { get; }
    Task CreateIndexesAsync(IMongoDatabase database);
}