
using Rezber.Domain.Models;

namespace Rezber.Services.Views.Admin;

public class AdminDashboardDto
{
    public int TotalPackages { get; set; }
    public int TotalUsers { get; set; }
    public long TotalDownloads { get; set; }
    public StorageSnapshot Storage { get; set; } = new() { Id = Guid.Empty };
    public List<Package> Packages { get; set; } = new();
    public List<UserDto> Users { get; set; } = new();
    public List<AuditLog> AuditLogs { get; set; } = new();
}

public static class AdminTabs
{
    public static string Active(string key) => key;
}

public class CleanupFormDto
{
    public int LogDays { get; set; } = 30;
    public bool Logs { get; set; } = true;
}

