namespace PhotoAI.Core.Interfaces;

public interface IImageCaptioner
{
    string ModelId { get; }
    string ModelName { get; }
    string Version { get; }
    bool IsLoaded { get; }

    Task<string> GenerateCaptionAsync(byte[] imageData, CancellationToken cancellationToken = default);
    Task<string> GenerateCaptionAsync(Stream imageStream, CancellationToken cancellationToken = default);
    Task LoadAsync(string modelPath, ProcessingBackend backend = ProcessingBackend.Auto, CancellationToken cancellationToken = default);
    void Unload();
}
