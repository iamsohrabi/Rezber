using MongoDB.Driver;

namespace Rezber.Core.MongoDB.Models;

/// <summary>
/// Interface for managing database indexes.
/// </summary>
public interface IIndexInitializer
{
    Task EnsureAllIndexesAsync(IMongoDatabase database);
}