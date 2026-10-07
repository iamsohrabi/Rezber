
using MongoDB.Bson.Serialization.Attributes;
using Rezber.Core.Domain;
using Rezber.Domain.Enums;

namespace Rezber.Domain.Models;

public class StorageSnapshot : AuditAggregate<Guid>
{
    [BsonElement("totalBytes")]
    public long TotalBytes { get; set; }

    [BsonElement("usedBytes")]
    public long UsedBytes { get; set; }

    [BsonElement("orphanBytes")]
    public long OrphanBytes { get; set; }

    [BsonElement("orphanFiles")]
    public int OrphanFiles { get; set; }

    [BsonElement("repositories")]
    public List<RepositoryInfo> Repositories { get; set; } = new();

    [BsonElement("blobStores")]
    public List<BlobStore> BlobStores { get; set; } = new();

    [BsonElement("breakdown")]
    public List<BreakdownItem> Breakdown { get; set; } = new();

    // ===== Computed =====
    public double UsagePercent =>
        TotalBytes == 0 ? 0 : (double)UsedBytes / TotalBytes * 100;

    public long FreeBytes => TotalBytes - UsedBytes;

    public string BarClass => UsagePercent switch
    {
        > 85 => "danger",
        > 60 => "warning",
        _ => ""
    };

    public string TotalFormatted => FormatBytes(TotalBytes);
    public string UsedFormatted => FormatBytes(UsedBytes);
    public string FreeFormatted => FormatBytes(FreeBytes);
    public string OrphanFormatted => FormatBytes(OrphanBytes);

    public static string FormatBytes(long bytes)
    {
        if (bytes < 0) return "0 B";

        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.#} {units[unit]}";
    }
}

public class RepositoryInfo
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("sizeBytes")]
    public long SizeBytes { get; set; }

    [BsonElement("fileCount")]
    public int FileCount { get; set; }

    [BsonElement("lastCleanup")]
    public DateTime LastCleanup { get; set; } = DateTime.UtcNow;

    [BsonElement("status")]
    public RepositoryStatus Status { get; set; } = RepositoryStatus.Healthy;

    public string StatusLabel => Status.ToPersianLabel();
    public string StatusColor => Status.ToCssColor();
    public string SizeFormatted => StorageSnapshot.FormatBytes(SizeBytes);
}

public class BlobStore
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("path")]
    public string Path { get; set; } = string.Empty;

    [BsonElement("sizeBytes")]
    public long SizeBytes { get; set; }

    [BsonElement("usagePercent")]
    public double UsagePercent { get; set; }

    [BsonElement("color")]
    public string Color { get; set; } = "info";

    public string SizeFormatted => StorageSnapshot.FormatBytes(SizeBytes);
}

public class BreakdownItem
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("icon")]
    public string Icon { get; set; } = "fa-cube";

    [BsonElement("sizeBytes")]
    public long SizeBytes { get; set; }

    [BsonElement("percent")]
    public double Percent { get; set; }

    [BsonElement("color")]
    public string Color { get; set; } = "info";

    public string SizeFormatted => StorageSnapshot.FormatBytes(SizeBytes);
}

