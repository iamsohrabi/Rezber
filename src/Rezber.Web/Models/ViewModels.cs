using Rezber.Services.Views.Admin;
using Rezber.Services.Views.Dashboard;
using Rezber.Services.Views.Packages;
using Rezber.Services.Features;
using System.ComponentModel.DataAnnotations;

namespace Rezber.Web.Models.ViewModels;

public sealed class PackageListViewModel : PackageListVDto
{
}

public sealed class PackageDetailViewModel : PackageDetailDto
{
}

public sealed class PackageFormViewModel : PackageFormDto
{
}

public sealed class PackageEditViewModel : PackageEditDto
{
}

public sealed class UserDashboardViewModel : UserDashboardDto
{
}

public sealed class AdminDashboardViewModel : AdminDashboardDto
{
}

public sealed class AdminNexusViewModel
{
	public NexusStatus? Status { get; set; }
	public bool? Writable { get; set; }
	public IReadOnlyCollection<NexusRepository> Repositories { get; set; } = Array.Empty<NexusRepository>();
	public NexusSearchResult? SearchResult { get; set; }
	public NexusRepository? Repository { get; set; }
	public NexusComponent? Component { get; set; }
	public NexusAsset? Asset { get; set; }
	public string? Query { get; set; }
	public string? SearchType { get; set; }
	public string? RepositoryFilter { get; set; }
	public string? FormatFilter { get; set; }
	public string? NameFilter { get; set; }
	public string? VersionFilter { get; set; }
	public string? GroupFilter { get; set; }
	public string? Error { get; set; }
}

public sealed class NexusSettingsViewModel
{
	public bool Enabled { get; set; }
	public string BaseUrl { get; set; } = string.Empty;
	public string? Repository { get; set; }
	public NexusStatus? Status { get; set; }
	public bool? Writable { get; set; }
	public string? Error { get; set; }
}

public sealed class ChangePasswordViewModel
{
	[Required]
	[DataType(DataType.Password)]
	public string CurrentPassword { get; set; } = string.Empty;

	[Required]
	[MinLength(8)]
	[DataType(DataType.Password)]
	public string NewPassword { get; set; } = string.Empty;

	[Required]
	[Compare(nameof(NewPassword))]
	[DataType(DataType.Password)]
	public string ConfirmPassword { get; set; } = string.Empty;
}
