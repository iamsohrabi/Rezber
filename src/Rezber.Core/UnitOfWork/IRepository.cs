using Rezber.Core.Domain;
using Rezber.Core.MongoDB;
using Rezber.Core.MongoDB.Models;

namespace Rezber.Core.UnitOfWork;

/// <summary>
/// Defines a generic repository interface for entities of type <typeparamref name="T"/> with identifier type <typeparamref name="TID"/>.
/// Extends <see cref="IReadOnlyRepository{T, TID}"/> and <see cref="IPersistentRepository{T, TID}"/> interfaces.
/// The <typeparamref name="T"/> entity must implement <see cref="IEntity{TID}"/>.
/// </summary>
public interface IRepository<T, TID> : IReadOnlyRepository<T, TID>,
     IPersistentRepository<T, TID> where T : AuditAggregate<TID>
{
    void CreateIndexes(IndexConfig indexConfig);
}

/// <summary>
/// Defines a generic repository interface for entities of type <typeparamref name="T"/> that inherits from the generic repository interface with identifier type <c>Guid</c>.
/// The <typeparamref name="T"/> entity must implement <see cref="IEntity"/>.
/// </summary>
public interface IRepository<T> : IRepository<T, Guid> where T : AuditAggregate<Guid> { }