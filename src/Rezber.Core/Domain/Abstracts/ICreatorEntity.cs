
namespace Rezber.Core.Domain.Abstract;

/// <summary>
/// Represents an entity that has creation metadata.
/// </summary>
/// <typeparam name="TId">The type of the identifier for the entity.</typeparam>
public interface ICreatorEntity<TId>
{
    /// <summary>
    /// Gets the date and time when the entity was created.
    /// </summary>
    DateTime Created { get; }

    /// <summary>
    /// Gets the identifier of the user who created the entity.
    /// </summary>
    TId? CreatedBy { get; }
}

/// <summary>
/// Represents an entity that has audit information including creation and modification metadata.
/// </summary>
/// <typeparam name="TId">The type of the identifier for the entity.</typeparam>
public interface IHaveAudit<TId> : ICreatorEntity<TId>
{
    /// <summary>
    /// Gets the date and time when the entity was last modified.
    /// </summary>
    DateTime LastModified { get; }

    /// <summary>
    /// Gets the identifier of the user who last modified the entity.
    /// </summary>
    TId? LastModifiedBy { get; }
}

/// <summary>
/// Represents an auditable entity with an identifier and audit information.
/// </summary>
/// <typeparam name="TId">The type of the identifier for the entity.</typeparam>
public interface IAuditableEntity<TId> : Rezber.Core.Domain.Abstract.IEntity<TId>, IHaveAudit<TId>
{
}

/// <summary>
/// Represents an auditable entity with a specific identity type and identifier type.
/// </summary>
/// <typeparam name="TIdentity">The type of the identity for the entity.</typeparam>
/// <typeparam name="TId">The type of the identifier for the entity.</typeparam>
public interface IAuditableEntity<TIdentity, TId> : IAuditableEntity<TIdentity>
    where TIdentity : IEntity<TId>
{
}

/// <summary>
/// Represents an auditable entity with a default identity type and a GUID identifier.
/// </summary>
public interface IAuditableEntity : IAuditableEntity<IEntity, Guid>
{
}

