
namespace Rezber.Core.Domain.Abstract;

/// <summary>
/// Represents an entity that supports soft deletion.
/// </summary>
public interface ISoftDeleteEntity
{
    /// <summary>
    /// Gets a value indicating whether the entity is marked as deleted.
    /// </summary>
    bool IsDeleted { get; set; }
}
