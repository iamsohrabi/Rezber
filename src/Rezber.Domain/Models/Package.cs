
using MongoDB.Bson.Serialization.Attributes;
using Rezber.Core.Domain;

namespace Rezber.Domain.Models;

public class Package : AuditAggregate<Guid>
{
    public string AuthorName => Author;
    public string AuthorJobTitle => AuthorRole;
    public DateTime UpdatedAt => LastModified;
    public string License { get; set; } = "MIT";
    public string LanguageKey => Lang;
    public string LanguageShort => Lang switch
    {
        "csharp" => "C#",
        "javascript" => "JS",
        "typescript" => "TS",
        _ => Lang.ToUpperInvariant()
    };
    public string LanguageFullName => Lang switch
    {
        "csharp" => "C# / .NET",
        "javascript" => "JavaScript",
        "typescript" => "TypeScript",
        "python" => "Python",
        "go" => "Go",
        _ => Lang
    };

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("version")]
    public string Version { get; set; } = "1.0.0";

    [BsonElement("lang")]
    public string Lang { get; set; } = "csharp";

    [BsonElement("target")]
    public string? Target { get; set; }

    [BsonElement("targetKey")]
    public string? TargetKey { get; set; }

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("imageWebp")]
    [BsonIgnoreIfNull]
    public byte[]? ImageWebp { get; set; }

    [BsonElement("imageLookupAttempted")]
    public bool ImageLookupAttempted { get; set; }

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = new();

    [BsonElement("downloads")]
    public int Downloads { get; set; }
    

    [BsonElement("author")]
    public string Author { get; set; } = string.Empty;

    [BsonElement("authorRole")]
    public string AuthorRole { get; set; } = "Developer";

    [BsonElement("authorPackages")]
    public int AuthorPackages { get; set; }


    [BsonElement("repository")]
    public string? Repository { get; set; }

    [BsonElement("readmeUrl")]
    public string? ReadmeUrl { get; set; }

    [BsonElement("readme")]
    public string? Readme { get; set; }

    [BsonElement("size")]
    public string Size { get; set; } = "0 MB";

    [BsonElement("installCommand")]
    public string InstallCommand { get; set; } = string.Empty;

    [BsonElement("dependencies")]
    public List<Dependency> Dependencies { get; set; } = new();

    [BsonElement("versions")]
    public List<PackageVersion> Versions { get; set; } = new();

    [BsonElement("changelog")]
    public List<Changelog> Changelog { get; set; } = new();

    [BsonElement("dailyDownloads")]
    public List<int> DailyDownloads { get; set; } = new();

    public void Update(Package package, string installCommand)
    {
        Name = package.Name;
        Version = package.Version;
        Lang = package.Lang;
        Target = package.Target;
        TargetKey = package.TargetKey;
        Description = package.Description;
        ImageWebp = package.ImageWebp;
        ImageLookupAttempted = package.ImageLookupAttempted;
        Tags = package.Tags;        
        License = package.License;
        Repository = package.Repository;
        ReadmeUrl = package.ReadmeUrl;
        Readme = package.Readme;
        Dependencies = package.Dependencies.Select(d => new Dependency
        {
            Name = d.Name, Version = d.Version, Type = d.Type
        }).ToList();

        InstallCommand = installCommand;
    }
}

public class Dependency
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Type { get; set; } = "runtime";
}

public class PackageVersion
{
    public string Version { get; set; } = "1.0.0";
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Size { get; set; } = "0 KB";
    public bool Latest { get; set; }
    public int Downloads { get; set; }
}

public class Changelog
{
    public string Version { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Body { get; set; } = string.Empty;
}
