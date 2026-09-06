namespace PhotoAI.Core.Models;

public class Person
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? RepresentativeFacePath { get; set; }
    public int PhotoCount { get; set; }
    public DateTime? FirstSeen { get; set; }
    public DateTime? LastSeen { get; set; }
    public DateTime DateCreated { get; set; }
    public string? Notes { get; set; }

    public ICollection<Face> Faces { get; set; } = new List<Face>();
}
