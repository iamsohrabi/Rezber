namespace Rezber.Core.MongoDB.Models;

/// <summary>
/// Defines a field in an index.
/// </summary>
public class IndexField
{
    public string FieldName { get; set; } = string.Empty;
    public IndexDirection Direction { get; set; } = IndexDirection.Ascending;
    public bool IsText { get; set; } = false;

    public static IndexField Ascending(string fieldName) =>
        new() { FieldName = fieldName, Direction = IndexDirection.Ascending };

    public static IndexField Descending(string fieldName) =>
        new() { FieldName = fieldName, Direction = IndexDirection.Descending };

    public static IndexField Text(string fieldName) =>
        new() { FieldName = fieldName, IsText = true };
}
