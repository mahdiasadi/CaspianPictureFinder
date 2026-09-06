namespace PhotoAI.Core.Interfaces;

public interface IOcrEngine
{
    string ModelId { get; }
    string ModelName { get; }
    string Version { get; }
    bool IsLoaded { get; }

    Task<IReadOnlyList<OcrDetectionResult>> RecognizeAsync(byte[] imageData, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OcrDetectionResult>> RecognizeAsync(Stream imageStream, CancellationToken cancellationToken = default);
    Task LoadAsync(string modelPath, ProcessingBackend backend = ProcessingBackend.Auto, CancellationToken cancellationToken = default);
    void Unload();
}

public class OcrDetectionResult
{
    public string Text { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public int BoundingBoxX { get; set; }
    public int BoundingBoxY { get; set; }
    public int BoundingBoxWidth { get; set; }
    public int BoundingBoxHeight { get; set; }
    public string? Language { get; set; }
}
