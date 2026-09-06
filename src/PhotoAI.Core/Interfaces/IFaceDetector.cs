using PhotoAI.Core.Models;

namespace PhotoAI.Core.Interfaces;

public interface IFaceDetector
{
    string ModelId { get; }
    string ModelName { get; }
    string Version { get; }
    bool IsLoaded { get; }

    Task<IReadOnlyList<FaceDetectionResult>> DetectAsync(byte[] imageData, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FaceDetectionResult>> DetectAsync(Stream imageStream, CancellationToken cancellationToken = default);
    Task LoadAsync(string modelPath, ProcessingBackend backend = ProcessingBackend.Auto, CancellationToken cancellationToken = default);
    void Unload();
}

public class FaceDetectionResult
{
    public int BoundingBoxX { get; set; }
    public int BoundingBoxY { get; set; }
    public int BoundingBoxWidth { get; set; }
    public int BoundingBoxHeight { get; set; }
    public float Confidence { get; set; }
}
