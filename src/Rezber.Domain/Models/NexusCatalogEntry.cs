using MongoDB.Bson.Serialization.Attributes;

namespace Rezber.Domain.Models;

public sealed class NexusCatalogEntry
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("repository")]
    public string Repository { get; set; } = string.Empty;

    [BsonElement("format")]
    public string Format { get; set; } = string.Empty;

    [BsonElement("group")]
    [BsonIgnoreIfNull]
    public string? Group { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("version")]
    public string Version { get; set; } = string.Empty;

    [BsonElement("searchText")]
    public string SearchText { get; set; } = string.Empty;

    [BsonElement("assetPaths")]
    public List<string> AssetPaths { get; set; } = new();

    [BsonElement("sizeBytes")]
    [BsonIgnoreIfNull]
    public long? SizeBytes { get; set; }

    [BsonElement("lastModified")]
    [BsonIgnoreIfNull]
    public DateTime? LastModified { get; set; }

    [BsonElement("indexedAt")]
    public DateTime IndexedAt { get; set; }
}