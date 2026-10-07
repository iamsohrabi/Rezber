using System.Net.Http.Headers;

public class UserDto
{
    public string Initials
    {
        get{
            var s = string.Concat(
                DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Take(1)
                    .Select(part => part[0]))
                    .ToUpperInvariant();
            
            return s;
        }
    }
    public string PrimaryRole => RoleLabel;
    public int PackageCount => Packages;
    public string Id { get; set; }  = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "user"; // "admin" | "user"
    public string RoleLabel { get; set; } = "Developer";
    public List<string> Roles { get; set; } = new();
    public int Packages { get; set; }
    public int StorageMb { get; set; }
    public DateTime LastActive { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; }
}