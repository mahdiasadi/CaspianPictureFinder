namespace PhotoAI.Core.Models;

public class IndexJob
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public IndexJobStatus Status { get; set; }
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int FailedFiles { get; set; }
    public int SkippedFiles { get; set; }
    public DateTime DateStarted { get; set; }
    public DateTime? DateCompleted { get; set; }
    public string? ErrorLog { get; set; }
    public string? CurrentPhase { get; set; }
}
