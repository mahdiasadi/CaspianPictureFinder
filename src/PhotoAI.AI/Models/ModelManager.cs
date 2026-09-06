using System.Text.Json;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.AI.Models;

public class ModelManager : IModelRegistry
{
    private readonly string _modelsDirectory;
    private readonly Dictionary<string, ModelInfo> _models = new();
    private readonly Dictionary<string, ModelMetadata> _metadata = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public ModelManager(string modelsDirectory)
    {
        _modelsDirectory = modelsDirectory;
        Directory.CreateDirectory(_modelsDirectory);
        LoadModels();
    }

    public IReadOnlyList<ModelInfo> GetAllModels() => _models.Values.ToList();

    public ModelInfo? GetModel(string modelId) => _models.TryGetValue(modelId, out var model) ? model : null;

    public bool IsModelInstalled(string modelId) => _models.ContainsKey(modelId);

    public async Task InstallModelAsync(string modelId, string sourcePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException($"Model file not found: {sourcePath}");

        var fileName = Path.GetFileName(sourcePath);
        var destPath = Path.Combine(_modelsDirectory, modelId, fileName);
        
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        File.Copy(sourcePath, destPath, true);

        var metadata = new ModelMetadata
        {
            ModelId = modelId,
            FileName = fileName,
            InstalledAt = DateTime.UtcNow,
            SourcePath = sourcePath,
            FileSizeBytes = new FileInfo(sourcePath).Length
        };

        await SaveMetadataAsync(modelId, metadata, cancellationToken);
        RefreshModelInfo(modelId, destPath);
    }

    public async Task RemoveModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (_models.TryGetValue(modelId, out var model))
        {
            var modelDir = Path.Combine(_modelsDirectory, modelId);
            if (Directory.Exists(modelDir))
            {
                Directory.Delete(modelDir, true);
            }

            _models.Remove(modelId);
            _metadata.Remove(modelId);
        }
    }

    public void EnableModel(string modelId)
    {
        if (_models.TryGetValue(modelId, out var model))
        {
            model.IsEnabled = true;
            SaveModelInfo(model);
        }
    }

    public void DisableModel(string modelId)
    {
        if (_models.TryGetValue(modelId, out var model))
        {
            model.IsEnabled = false;
            SaveModelInfo(model);
        }
    }

    public string GetModelPath(string modelId)
    {
        if (_models.TryGetValue(modelId, out var model))
            return model.Path ?? string.Empty;
        return string.Empty;
    }

    private void LoadModels()
    {
        if (!Directory.Exists(_modelsDirectory)) return;

        foreach (var dir in Directory.GetDirectories(_modelsDirectory))
        {
            var modelId = Path.GetFileName(dir);
            var metaPath = Path.Combine(dir, "metadata.json");
            
            if (File.Exists(metaPath))
            {
                try
                {
                    var json = File.ReadAllText(metaPath);
                    var metadata = JsonSerializer.Deserialize<ModelMetadata>(json, _jsonOptions);
                    if (metadata != null)
                    {
                        _metadata[modelId] = metadata;
                    }
                }
                catch { }
            }

            var files = Directory.GetFiles(dir, "*.onnx");
            if (files.Length > 0)
            {
                RefreshModelInfo(modelId, files[0]);
            }
        }
    }

    private void RefreshModelInfo(string modelId, string modelPath)
    {
        var fileInfo = new FileInfo(modelPath);
        var metadata = _metadata.TryGetValue(modelId, out var meta) ? meta : new ModelMetadata { ModelId = modelId };

        var modelInfo = new ModelInfo
        {
            ModelId = modelId,
            Name = metadata.Name ?? modelId,
            Version = metadata.Version ?? "1.0",
            Purpose = metadata.Purpose ?? "Unknown",
            Path = modelPath,
            License = metadata.License ?? "Unknown",
            SizeBytes = fileInfo.Length,
            SupportsCpu = true,
            SupportsCuda = true,
            IsInstalled = true,
            IsEnabled = metadata.IsEnabled,
            EmbeddingDimension = metadata.EmbeddingDimension
        };

        _models[modelId] = modelInfo;
    }

    private void SaveModelInfo(ModelInfo model)
    {
        if (_metadata.TryGetValue(model.ModelId, out var metadata))
        {
            metadata.Name = model.Name;
            metadata.Version = model.Version;
            metadata.Purpose = model.Purpose;
            metadata.License = model.License;
            metadata.IsEnabled = model.IsEnabled;
            metadata.EmbeddingDimension = model.EmbeddingDimension;
            _ = SaveMetadataAsync(model.ModelId, metadata, CancellationToken.None);
        }
    }

    private async Task SaveMetadataAsync(string modelId, ModelMetadata metadata, CancellationToken cancellationToken)
    {
        var modelDir = Path.Combine(_modelsDirectory, modelId);
        Directory.CreateDirectory(modelDir);
        
        var metaPath = Path.Combine(modelDir, "metadata.json");
        var json = JsonSerializer.Serialize(metadata, _jsonOptions);
        await File.WriteAllTextAsync(metaPath, json, cancellationToken);
    }
}

public class ModelMetadata
{
    public string ModelId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Purpose { get; set; }
    public string? License { get; set; }
    public string? FileName { get; set; }
    public DateTime InstalledAt { get; set; }
    public string? SourcePath { get; set; }
    public long FileSizeBytes { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int? EmbeddingDimension { get; set; }
}