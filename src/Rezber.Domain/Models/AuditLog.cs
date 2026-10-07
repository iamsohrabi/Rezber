
using MongoDB.Bson.Serialization.Attributes;
using Rezber.Core.Domain;
using Rezber.Domain.Abstracts;

namespace Rezber.Domain.Models;

public class AuditLog : AuditAggregate<Guid>
{
    public DateTime Time => Created;
    [BsonElement("userId")]
    public Guid UserId { get; set; }

    [BsonElement("userName")]
    public string UserName { get; set; } = string.Empty;

    [BsonElement("action")]
    public AuditActionType Action { get; set; }

    [BsonElement("target")]
    public string Target { get; set; } = string.Empty;

    [BsonElement("ip")]
    public string Ip { get; set; } = string.Empty;
    
    public string ActionLabel => Action.ToPersianLabel();
    public string ActionColor => Action.ToCssColor();
}

