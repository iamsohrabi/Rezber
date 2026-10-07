
using Rezber.Domain.Models;

namespace Rezber.Services.Views.Dashboard;

public class UserDashboardDto
{
    public UserDto User { get; set; } = new();
    public List<Package> MyPackages { get; set; } = new();
    public int TotalDownloads { get; set; }
    public double AverageDownloadsPerPackage { get; set; }
    public int TotalVersions { get; set; }
    public string? MostDownloadedPackage { get; set; }
    public double UsedStorageMb { get; set; }
    public Package? LastPublished { get; set; }
}
