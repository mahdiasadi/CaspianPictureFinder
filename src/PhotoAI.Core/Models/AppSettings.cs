namespace PhotoAI.Core.Models;

public class AppSettings
{
    public long Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DateModified { get; set; }
}
