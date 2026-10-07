using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;

namespace Rezber.Domain.Identity;

[CollectionName("roles")]
public class Role : MongoIdentityRole<Guid>
{
    public Role() : base()
    {
    }

    public Role(string roleName) : base(roleName)
    {
    }
}
