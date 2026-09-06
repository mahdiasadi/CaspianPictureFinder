namespace PhotoAI.Core.Interfaces;

public interface ISceneClassifier
{
    string ModelId { get; }
    string ModelName { get; }
    string Version { get; }
    bool IsLoaded { get; }

    Task<IReadOnlyList<SceneClassificationResult>> ClassifyAsync(byte[] imageData, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SceneClassificationResult>> ClassifyAsync(Stream imageStream, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetSupportedScenesAsync();
    Task LoadAsync(string modelPath, ProcessingBackend backend = ProcessingBackend.Auto, CancellationToken cancellationToken = default);
    void Unload();
}

public class SceneClassificationResult
{
    public string Label { get; set; } = string.Empty;
    public float Confidence { get; set; }
}
