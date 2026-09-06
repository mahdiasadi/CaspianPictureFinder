# PhotoAI - Developer Guide

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Project Structure](#project-structure)
3. [Adding New AI Models](#adding-new-ai-models)
4. [Extending Search](#extending-search)
5. [Adding New Repository](#adding-new-repository)
6. [Testing](#testing)
7. [Benchmarking](#benchmarking)
8. [Code Style](#code-style)
9. [CI/CD](#cicd)

---

## Architecture Overview

### Clean Architecture Layers

```
┌─────────────────────────────────────┐
│         PhotoAI.App (WPF)           │  ← Presentation Layer
├─────────────────────────────────────┤
│      PhotoAI.Application            │  ← Use Cases / Application Services
├─────────────────────────────────────┤
│           PhotoAI.Core              │  ← Domain Models & Interfaces
├─────────────────────────────────────┤
│  PhotoAI.AI  │  PhotoAI.Media  ...  │  ← Infrastructure / AI / Media
├─────────────────────────────────────┤
│         PhotoAI.Storage             │  ← Data Access (EF Core + SQLite)
└─────────────────────────────────────┘
```

### Dependency Rules
- Inner layers don't know about outer layers
- Dependencies point inward
- Interfaces in Core, implementations in Infrastructure

### Key Principles
- **Dependency Inversion** - High-level modules don't depend on low-level
- **Interface Segregation** - Small, focused interfaces
- **Single Responsibility** - Each class has one reason to change
- **Open/Closed** - Open for extension, closed for modification

---

## Project Structure

```
PhotoAI.sln
├── src/
│   ├── PhotoAI.App/              # WPF Application
│   │   ├── ViewModels/           # MVVM ViewModels
│   │   ├── Views/                # XAML Views
│   │   ├── Converters/           # Value Converters
│   │   └── App.xaml.cs           # DI Registration
│   ├── PhotoAI.Core/             # Domain Core
│   │   ├── Models/               # Domain Entities
│   │   ├── Interfaces/           # Repository & AI Interfaces
│   │   ├── Enums/                # Domain Enums
│   │   └── Inference/            # ONNX Runtime Abstraction
│   ├── PhotoAI.Application/      # Use Cases
│   ├── PhotoAI.Infrastructure/   # Cross-cutting
│   ├── PhotoAI.AI/               # AI Module (DI Registration)
│   ├── PhotoAI.AI.Vision/        # Vision Models
│   │   ├── SigLipImageEmbeddingModel.cs
│   │   ├── SigLipTextEmbeddingModel.cs
│   │   ├── RTDETRv2ObjectDetector.cs
│   │   ├── Places365SceneClassifier.cs
│   │   ├── MoondreamCaptioner.cs
│   │   └── DocumentDetector.cs
│   ├── PhotoAI.AI.Face/          # Face Models
│   │   ├── ScrfdFaceDetector.cs
│   │   └── SFaceEmbeddingModel.cs
│   ├── PhotoAI.AI.OCR/           # OCR
│   │   └── PaddleOcrEngine.cs
│   ├── PhotoAI.Media/            # Media Processing
│   │   ├── Processing/
│   │   │   ├── ImageProcessor.cs
│   │   │   ├── MetadataExtractor.cs
│   │   │   └── VideoProcessor.cs
│   ├── PhotoAI.Search/           # Search Engine
│   │   ├── SearchRanker.cs
│   │   ├── HybridSearchService.cs
│   │   ├── TextSearch.cs
│   │   ├── SimilarImageSearch.cs
│   │   └── VideoSearch.cs
│   ├── PhotoAI.Vector/           # Vector Store
│   │   ├── LocalVectorStore.cs
│   │   └── QdrantVectorStore.cs
│   ├── PhotoAI.Indexing/         # Indexing Pipeline
│   │   ├── Pipeline/
│   │   │   ├── FileScanner.cs
│   │   │   ├── MetadataWorker.cs
│   │   │   ├── ThumbnailWorker.cs
│   │   │   ├── FaceDetectionWorker.cs
│   │   │   ├── ObjectDetectionWorker.cs
│   │   │   ├── SceneClassificationWorker.cs
│   │   │   ├── VideoFrameEmbeddingWorker.cs
│   │   │   ├── OcrWorker.cs
│   │   │   ├── CaptionWorker.cs
│   │   │   └── SmartAlbumWorker.cs
│   │   └── IndexingPipeline.cs
│   ├── PhotoAI.Storage/          # Data Access
│   │   ├── Data/PhotoAiDbContext.cs
│   │   └── Repositories/
│   └── PhotoAI.Vector/           # Vector Search
├── tests/
│   ├── PhotoAI.Core.Tests/
│   ├── PhotoAI.Application.Tests/
│   ├── PhotoAI.Infrastructure.Tests/
│   └── PhotoAI.Indexing.Tests/
├── PhotoAI.Packaging/            # MSIX Packaging
├── PhotoAI.Benchmarks/           # BenchmarkDotNet
├── Package.appxmanifest          # MSIX Manifest
├── UserGuide.md
├── DeveloperGuide.md
├── API.md
├── PHASE9.md
├── MODEL-LICENSE.md
└── ARCHITECTURE.md
```

---

## Adding New AI Models

### 1. Define Interface (in PhotoAI.Core/Interfaces)

```csharp
// IMyNewModel.cs
namespace PhotoAI.Core.Interfaces;

public interface IMyNewModel
{
    string ModelId { get; }
    string ModelName { get; }
    string Version { get; }
    bool IsLoaded { get; }

    Task<MyResult> ProcessAsync(byte[] imageData, CancellationToken cancellationToken = default);
    Task LoadAsync(string modelPath, ProcessingBackend backend = ProcessingBackend.Auto, CancellationToken cancellationToken = default);
    void Unload();
}

// MyResult.cs
public class MyResult
{
    public float Confidence { get; set; }
    // ... other properties
}
```

### 2. Implement Model (in PhotoAI.AI.Vision or new project)

```csharp
// MyNewModel.cs
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using PhotoAI.Core.Inference;
using PhotoAI.Core.Interfaces;
using SixLabors.ImageSharp;

namespace PhotoAI.AI.Vision;

public class MyNewModel : IMyNewModel
{
    private readonly IInferenceEngine _engine;
    private IInferenceSession? _session;
    private bool _disposed;

    public string ModelId => "my-new-model";
    public string ModelName => "My New Model";
    public string Version => "1.0";
    public bool IsLoaded => _session != null;

    public MyNewModel(IInferenceEngine engine)
    {
        _engine = engine;
    }

    public async Task LoadAsync(string modelPath, ProcessingBackend backend = ProcessingBackend.Auto, CancellationToken cancellationToken = default)
    {
        if (_session != null) return;

        var selectedEngine = backend switch
        {
            ProcessingBackend.Cuda => InferenceEngineFactory.Create(ProcessingBackend.Cuda),
            ProcessingBackend.Cpu => InferenceEngineFactory.Create(ProcessingBackend.Cpu),
            _ => _engine
        };

        _session = await selectedEngine.CreateSessionAsync(modelPath, cancellationToken: cancellationToken);
    }

    public async Task<MyResult> ProcessAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        EnsureLoaded();
        
        var inputTensor = await PreprocessAsync(imageData, cancellationToken);
        var result = await RunInferenceAsync(inputTensor, cancellationToken);
        return PostProcess(result);
    }

    private async Task<float[]> PreprocessAsync(byte[] imageData, CancellationToken cancellationToken)
    {
        using var image = Image.Load<Rgba32>(imageData);
        image.Mutate(x => x.Resize(224, 224));
        
        var tensor = new float[3 * 224 * 224];
        int idx = 0;
        
        image.ProcessPixelRows(accessor =>
        {
            for (int c = 0; c < 3; c++)
            {
                for (int y = 0; y < 224; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < 224; x++)
                    {
                        var pixel = row[x];
                        float val = c switch
                        {
                            0 => pixel.R / 255f,
                            1 => pixel.G / 255f,
                            _ => pixel.B / 255f
                        };
                        tensor[idx++] = (val - 0.485f) / 0.229f; // ImageNet norm
                    }
                }
            }
        });
        
        return tensor;
    }

    private async Task<MyResult> RunInferenceAsync(float[] input, CancellationToken cancellationToken)
    {
        var inputName = _session!.InputNames[0];
        var inputTensor = NamedOnnxValue.CreateFromTensor(
            _session.InputNames[0],
            new DenseTensor<float>(input, new[] { 1, 3, 224, 224 }));

        var inputs = new[] { inputTensor };
        var result = await _session.RunAsync(inputs, cancellationToken);
        
        var output = result.Outputs[0].AsTensor<float>();
        return new MyResult { Confidence = output.Max() };
    }

    private void EnsureLoaded()
    {
        if (_session == null)
            throw new InvalidOperationException("Model not loaded. Call LoadAsync first.");
    }

    public void Unload() { _session?.Dispose(); _session = null; }
    public void Dispose() { if (!_disposed) { _session?.Dispose(); _disposed = true; } }
}
```

### 3. Register in DI (PhotoAI.AI/ServiceCollectionExtensions.cs)

```csharp
services.AddTransient<IMyNewModel>(sp => new MyNewModel(sp.GetRequiredService<IInferenceEngine>()));
```

### 4. Add to Model Registry (ModelManager)

```csharp
// In ModelManager.InstallModelAsync or similar
// Model metadata includes: ModelId, Name, Version, Purpose, License, Size, EmbeddingDimension
```

### 5. Add to Pipeline (if needed)

```csharp
// In IndexingPipeline.cs or new worker
services.AddSingleton<MyNewWorker>();
```

---

## Extending Search

### Adding New Ranking Signal

1. **Add to SearchQuery** (SearchRanker.cs)
```csharp
public class SearchQuery
{
    // ... existing properties
    public IReadOnlyList<string>? MyNewFilter { get; set; }
}
```

2. **Add Weight** (RankingWeights)
```csharp
public class RankingWeights
{
    // ... existing
    public float MyNewSignal { get; set; } = 0.05f;
}
```

3. **Add Scoring Method** in SearchRanker
```csharp
private float CalculateMyNewSignalScore(long mediaItemId, SearchQuery? query, Dictionary<long, List<MyData>> allData)
{
    if (query?.MyNewFilter?.Count == 0 || !allData.TryGetValue(mediaItemId, out var data))
        return 0f;

    // Calculate score logic
    return score;
}
```

4. **Add to Score Calculation**
```csharp
var myNewScore = CalculateMyNewSignalScore(mediaItemId, query, allMyData);
scoreBreakdown["myNewSignal"] = myNewScore;

var finalScore = 
    // ... existing
    + myNewScore * _weights.MyNewSignal;
```

5. **Add Weight to Total**
```csharp
var totalWeight = weights.Semantic + weights.Object + // ... + weights.MyNewSignal;
```

5. **Update HybridSearchService** to pass new filter to RankResultsAsync

---

## Adding New Repository

### 1. Define Interface (PhotoAI.Core/Interfaces)
```csharp
public interface IMyNewRepository
{
    Task<MyEntity?> GetByIdAsync(long id);
    Task<IReadOnlyList<MyEntity>> GetByMediaItemIdAsync(long mediaItemId);
    Task AddAsync(MyEntity entity);
    Task AddRangeAsync(IEnumerable<MyEntity> entities);
    Task DeleteByMediaItemIdAsync(long mediaItemId);
}
```

### 2. Implement (PhotoAI.Storage/Repositories)
```csharp
public class MyNewRepository : IMyNewRepository
{
    private readonly Func<PhotoAiDbContext> _contextFactory;
    
    public MyNewRepository(Func<PhotoAiDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
    
    // Implement methods using EF Core
}
```

### 3. Register in Storage DI
```csharp
// PhotoAI.Storage/ServiceCollectionExtensions.cs
services.AddScoped<IMyNewRepository, MyNewRepository>();
```

### 4. Update DbContext
```csharp
// PhotoAiDbContext.cs
public DbSet<MyEntity> MyEntities => Set<MyEntity>();

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<MyEntity>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedOnAdd();
        entity.HasIndex(e => e.MediaItemId);
        entity.HasOne(e => e.MediaItem).WithMany(m => m.MyEntities).HasForeignKey(e => e.MediaItemId);
    });
}
```

---

## Testing

### Unit Tests (PhotoAI.Core.Tests)
```csharp
public class ImageProcessorTests
{
    [Fact]
    public void ComputeSimilarity_SameHash_Returns1()
    {
        var processor = new ImageProcessor();
        ulong hash = 0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101010;
        var similarity = ImageProcessor.ComputeSimilarity(hash, hash);
        Assert.Equal(1.0, similarity);
    }
}
```

### Integration Tests (PhotoAI.Indexing.Tests)
```csharp
public class IndexingPipelineTests
{
    [Fact]
    public async Task IndexingPipeline_ProcessesImagesCorrectly()
    {
        // Arrange: Create test images
        // Act: Run pipeline
        // Assert: MediaItems created with metadata
    }
}
```

### Running Tests
```bash
dotnet test PhotoAI.sln -c Release
dotnet test --filter "FullyQualifiedName~ImageProcessor"
dotnet test --logger "console;verbosity=detailed"
```

---

## Benchmarking

### Project Structure
```
PhotoAI.Benchmarks/
├── PhotoAI.Benchmarks.csproj
├── InferenceBenchmarks.cs
├── IndexingBenchmarks.cs
├── SearchBenchmarks.cs
└── Program.cs
```

### InferenceBenchmarks.cs
```csharp
using BenchmarkDotNet.Attributes;
using PhotoAI.AI.Vision;
using PhotoAI.Core.Inference;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class InferenceBenchmarks
{
    private IInferenceEngine _cpuEngine;
    private IInferenceEngine _gpuEngine;
    private SigLipImageEmbeddingModel _cpuModel;
    private SigLipImageEmbeddingModel _gpuModel;
    private byte[] _testImage;

    [GlobalSetup]
    public async Task Setup()
    {
        _cpuEngine = new CpuInferenceEngine();
        _gpuEngine = new CudaInferenceEngine();
        
        _cpuModel = new SigLipImageEmbeddingModel(_cpuEngine);
        _gpuModel = new SigLipImageEmbeddingModel(_gpuEngine);
        
        await _cpuModel.LoadAsync("models/siglip2.onnx", ProcessingBackend.Cpu);
        await _gpuModel.LoadAsync("models/siglip2.onnx", ProcessingBackend.Cuda);
        
        _testImage = File.ReadAllBytes("test.jpg");
    }

    [Benchmark]
    public async Task<float[]> SigLip_CPU_Single()
    {
        return await _cpuModel.GetEmbeddingAsync(_testImage);
    }

    [Benchmark]
    public async Task<float[]> SigLip_GPU_Single()
    {
        return await _gpuModel.GetEmbeddingAsync(_testImage);
    }

    [Benchmark]
    public async Task<float[][]> SigLip_CPU_Batch16()
    {
        var images = Enumerable.Repeat(_testImage, 16).ToArray();
        return await _cpuModel.GetEmbeddingsBatchAsync(images);
    }

    [Benchmark]
    public async Task<float[][]> SigLip_GPU_Batch32()
    {
        var images = Enumerable.Repeat(_testImage, 32).ToArray();
        return await _gpuModel.GetEmbeddingsBatchAsync(images);
    }
}
```

### Running Benchmarks
```bash
dotnet run -c Release -p PhotoAI.Benchmarks/PhotoAI.Benchmarks.csproj
```

### Output
- Console output
- HTML report in `BenchmarkDotNet.Artifacts/`
- CSV/JSON export available

---

## Code Style

### C# Conventions
- **Nullable Reference Types**: Enabled (`<Nullable>enable</Nullable>`)
- **Implicit Usings**: Enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- **File-scoped namespaces**: `namespace PhotoAI.Core;`
- **Primary constructors** where applicable
- **Record types** for DTOs
- **Pattern matching** over if/else chains

### Naming
| Element | Convention |
|---------|------------|
| Classes/Interfaces | PascalCase |
| Methods/Properties | PascalCase |
| Parameters/Locals | camelCase |
| Constants | UPPER_SNAKE_CASE |
| Private fields | _camelCase |
| Interfaces | IPascalCase |

### Async/Await
- Always `async Task` not `async void`
- `CancellationToken` parameters
- `ConfigureAwait(false)` in library code

### Error Handling
- No silent failures - log and rethrow
- Result<T> pattern for expected failures
- Exceptions for unexpected failures

---

## CI/CD

### GitHub Actions Workflow
```yaml
# .github/workflows/ci.yml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
          
      - name: Restore
        run: dotnet restore PhotoAI.sln
        
      - name: Build
        run: dotnet build PhotoAI.sln --no-restore -c Release
        
      - name: Test
        run: dotnet test PhotoAI.sln --no-restore -c Release --logger "trx"
        
      - name: Upload Test Results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: **/*.trx

  benchmark:
    runs-on: windows-latest
    needs: build
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet run -c Release -p PhotoAI.Benchmarks/PhotoAI.Benchmarks.csproj
      - uses: actions/upload-artifact@v4
        with:
          name: benchmark-results
          path: BenchmarkDotNet.Artifacts/**

  package:
    runs-on: windows-latest
    needs: build
    if: github.event_name == 'push' && github.ref == 'refs/tags/'
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
      - run: dotnet msbuild PhotoAI.Packaging/PhotoAI.Packaging.csproj -t:Build -p:Configuration=Release -p:Platform=x64
      - uses: actions/upload-artifact@v4
        with:
          name: msix-package
          path: artifacts/msix/
```

---

## License Compliance

See `MODEL-LICENSE.md` for model license tracking.

### Checklist Before Release
- [ ] All models have permissive licenses
- [ ] No GPL/AGPL dependencies
- [ ] License attributions in About dialog
- [ ] Third-party licenses in LICENSES folder
- [ ] SPDX headers in source files

---

## Release Process

1. **Version Bump**
   ```bash
   # Update version in all .csproj files
   # Update CHANGELOG.md
   git tag v1.0.0
   git push origin v1.0.0
   ```

2. **GitHub Actions** builds MSIX
2. **GitHub Release** created with MSIX bundle
3. **AppInstaller** updated for auto-updates
4. **Docker** image published (optional)

---

## Contributing

1. Fork repository
2. Create feature branch
3. Write tests
4. Ensure build passes
5. Submit PR with description
6. Code review
7. Merge to develop/main

---

*Generated as part of PhotoAI Phase 9*