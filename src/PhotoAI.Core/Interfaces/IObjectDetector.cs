using PhotoAI.Core.Models;

namespace PhotoAI.Core.Interfaces;

public interface IObjectDetector
{
    string ModelId { get; }
    string ModelName { get; }
    string Version { get; }
    bool IsLoaded { get; }

    Task<IReadOnlyList<ObjectDetectionResult>> DetectAsync(byte[] imageData, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ObjectDetectionResult>> DetectAsync(Stream imageStream, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetSupportedLabelsAsync();
    Task LoadAsync(string modelPath, ProcessingBackend backend = ProcessingBackend.Auto, CancellationToken cancellationToken = default);
    void Unload();
}

public class ObjectDetectionResult
{
    public string Label { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public int BoundingBoxX { get; set; }
    public int BoundingBoxY { get; set; }
    public int BoundingBoxWidth { get; set; }
    public int BoundingBoxHeight { get; set; }
}
