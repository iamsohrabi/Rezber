namespace Rezber.Core.MongoDB.Models;

public class IndexConfig
{
    public string? Name { get; set; }
    public bool IsUnique { get; set; } = false;
    public bool IsText { get; set; } = false;
    public List<IndexField> Fields { get; set; } = new();

    // Kept for backward compatibility.
    public List<string> Ascending { get; set; } = new();

    public static IndexConfig CreateTextIndex(string name, List<string> textFields)
    {
        var config = new IndexConfig { Name = name, IsText = true };
        foreach (var field in textFields)
        {
            config.Fields.Add(IndexField.Text(field));
        }
        return config;
    }

    public static IndexConfig CreateComposite(string name, List<IndexField> fields, bool isUnique = false)
    {
        return new IndexConfig
        {
            Name = name,
            IsUnique = isUnique,
            Fields = fields
        };
    }
}