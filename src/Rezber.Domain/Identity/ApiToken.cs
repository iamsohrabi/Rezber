using MongoDbGenericRepository.Attributes;
using Rezber.Core.Domain;

namespace Rezber.Domain.Identity;

[CollectionName("apiTokens")]
public sealed class ApiToken : AuditAggregate<Guid>
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string TokenPrefix { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}