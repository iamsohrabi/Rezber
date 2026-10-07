
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Rezber.Core.Domain.Abstract;

/// <summary>
/// Defines a generic interface for entities with an identifier property of type TID.
/// Entities implementing this interface can be used in repositories and domain models 
/// that work with generic identifiers.
/// Defines a generic interface for entities with an identifier property of type TID.
/// <typeparam name="TID">An id type for database key</typeparam>
/// </summary>
public interface IEntity<TID>
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    TID Id { get; set; }
}

///<summary>
/// Defines a base entity interface with a GUID identifier. Entities implementing 
/// this interface can be used in repositories and domain models that work with 
/// GUID identifiers. 
/// Defines a base entity interface with a GUID identifier. Entities implementing <
/// </summary>
public interface IEntity : IEntity<Guid>
{ }
