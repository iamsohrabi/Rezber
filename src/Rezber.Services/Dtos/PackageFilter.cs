public class PackageFilter
{
    public string? Query { get; set; }
    public string Filter { get; set; } = "all";
    public string Sort { get; set; } = "recent";
    public string? Target { get; set; }
    public string? License { get; set; }
    public bool OnlyMine { get; set; }
    public bool OnlyRecent { get; set; }
    public string CurrentUser { get; set; } = string.Empty;
}
