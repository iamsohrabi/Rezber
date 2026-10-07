public class PackageDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;   
    public string Version { get; set; } = string.Empty;    
    public string Lang { get; set; } = string.Empty;    
    public string? Target { get; set; }    
    public string? TargetKey { get; set; }    
    public string Description { get; set; } = string.Empty;        
    public int Downloads { get; set; }
    public string License { get; set; } = "MIT";
    public List<string> Tags { get; set; } = new();
    public string? Repository { get; set; }
    public string Author { get; set; } = string.Empty;    
    public string? Readme { get; set; }    
    public string Size { get; set; } = string.Empty;    
    public string InstallCommand { get; set; } = string.Empty;
    public bool HasImage { get; set; }
}
