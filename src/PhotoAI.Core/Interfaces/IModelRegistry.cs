namespace PhotoAI.Core.Interfaces;

public interface IModelRegistry
{
    IReadOnlyList<ModelInfo> GetAllModels();
    ModelInfo? GetModel(string modelId);
    bool IsModelInstalled(string modelId);
    Task InstallModelAsync(string modelId, string sourcePath, CancellationToken cancellationToken = default);
    Task RemoveModelAsync(string modelId, CancellationToken cancellationToken = default);
    void EnableModel(string modelId);
    void DisableModel(string modelId);
    string GetModelPath(string modelId);
}
