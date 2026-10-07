using Rezber.Core.Domain.Abstract;
using MongoDB.Bson.Serialization.Attributes;

namespace Rezber.Core.Domain;


/// <summary>
/// Represents an abstract base class for an auditable aggregate root entity.
/// </summary>
/// <typeparam name="TId">The type of the identifier for the entity.</typeparam>
public abstract class AuditAggregate<TId> :
    IEntity<TId>,
    IAuditableEntity<TId>,
    ISoftDeleteEntity
{

    /// <summary>
    /// Marks the entity as soft deleted and updates the <see cref="LastModified"/> and <see cref="LastModifiedBy"/> properties.
    /// </summary>
    /// <param name="deletedBy">The identifier of the user or process that deleted the entity.</param>
    public void SoftDelete(TId deletedBy)
    {
        if (!IsDeleted)
        {
            IsDeleted = true;
            LastModified = DateTime.UtcNow;
            LastModifiedBy = deletedBy;
        }
    }

    public static Guid MakeId()
        => Guid.NewGuid();

    public required TId Id { get; set; }
    public DateTime Created { get; set; }
    public TId? CreatedBy { get; set; }
    public TId? LastModifiedBy { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsDeleted { get; set; }
}


public interface ILocalization<T>
{
    Dictionary<string, T> Contents { get; set; }
    List<string> AvailableLanguages { get; set; }

    /// <summary>
    /// Gets content in a specific language.
    /// </summary>
    /// <param name="lang">Requested language.</param>
    /// <returns>Translated content in the target language.</returns>
    T? GetLocalizedContent(string lang);

    /// <summary>
    /// Gets content in the default language.
    /// </summary>
    /// <param name="lang">Default language.</param>
    /// <returns>Translated content in the target language.</returns>
    T? GetDefaultContent(string lang);

    /// <summary>
    /// Checks whether a language exists.
    /// </summary>
    /// <param name="lang">Language to check.</param>
    /// <returns>True when the language exists; otherwise false.</returns>
    bool HasLanguage(string lang);
}