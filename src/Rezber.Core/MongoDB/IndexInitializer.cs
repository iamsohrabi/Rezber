using Rezber.Core.MongoDB.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;


namespace Rezber.Core.MongoDB;

/// <summary>
/// Centralized database index management.
/// </summary>
public class IndexInitializer : IIndexInitializer
{
    private readonly ILogger<IndexInitializer> _logger;
    private readonly IEnumerable<IIndexCreator> _indexCreators;

    public IndexInitializer(ILogger<IndexInitializer> logger,
                            IEnumerable<IIndexCreator> indexCreators)
    {
        _logger = logger;
        _indexCreators = indexCreators;
    }

    public async Task EnsureAllIndexesAsync(IMongoDatabase database)
    {
        _logger.LogInformation("Starting index creation for all collections...");

        foreach (var creator in _indexCreators)
        {
            await creator.CreateIndexesAsync(database);
        }

        _logger.LogInformation("All indexes created successfully.");
    }
}
