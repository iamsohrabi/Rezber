using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;

namespace Rezber.Domain.Identity;

[CollectionName("users")]
public class User : MongoIdentityUser<Guid>
{
    public User() : base()
    {
    }

    public User(string userName, string email) : base(userName, email)
    {
    }

    public string DisplayName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = "Developer";
    public DateTime LastActive { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public int PackageCount { get; set; }
    public int StorageMb { get; set; }
}
