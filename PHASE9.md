# PhotoAI - Phase 9: Production Ready

## Summary

### Phase 9 Goals
1. **MSIX Installer** - Package as Windows Store-ready installer
2. **Model Management UI** - Download/install/remove AI models
3. **Settings Persistence** - All user preferences saved
4. **Documentation** - User guide, developer guide, API docs
5. **Benchmarking** - Performance tests for inference, indexing, search

---

## 1. MSIX Installer

### Project Structure
```
PhotoAI.Packaging/
├── PhotoAI.Packaging.csproj
├── Package.appxmanifest
└── Assets/
    ├── Square44x44Logo.png
    ├── Square150x150Logo.png
    ├── Wide310x150Logo.png
    ├── Square44x44Logo.scale-200.png
    └── Square150x150Logo.scale-200.png
```

### Build Commands
```bash
# Build MSIX
dotnet msbuild PhotoAI.Packaging/PhotoAI.Packaging.csproj -t:Build -p:Configuration=Release

# Create test certificate (for sideloading)
New-SelfSignedCertificate -Type Custom -Subject "CN=PhotoAI" -KeyUsage DigitalSignature -FriendlyName "PhotoAI Test Cert" -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={critical}{text}ca=false")

# Sign package (requires certificate)
signtool sign /fd SHA256 /a PhotoAI_1.0.0.0_x64.msixbundle
```

### Package.appxmanifest Key Points
- `runFullTrust` capability for full file system access
- `documentsLibrary`, `picturesLibrary`, `videosLibrary` for media access
- `internetClient` only for optional model downloads (disabled by default)
- Wide310x150Logo for live tiles

### Distribution
- GitHub Releases for distribution
- AppInstaller for auto-updates
- Code signing required for production

---

## 2. Model Management UI

### Features
- **Model Catalog** - List available models with versions, sizes, licenses
- **Install/Remove** - Download from GitHub Releases or local files
- **Enable/Disable** - Toggle models without uninstalling
- **Version Management** - Switch between model versions
- **License Display** - Show license info before install
- **Storage Usage** - Show disk space per model
- **Integrity Check** - Verify model file hashes

### UI Components
```
Settings > Models
├── Model List (Virtualized)
│   ├── Model Card
│   │   ├── Name, Version, Size
│   │   ├── Purpose (Embedding/Face/Object/OCR/Caption)
│   │   ├── License Badge (Apache 2.0/MIT/CC-BY)
│   │   ├── Status (Installed/Not Installed/Update Available)
│   │   ├── Actions [Install/Remove/Enable/Disable/Update]
│   │   └── License Details (Expandable)
│   └── "Add Local Model" Button
└── Storage Summary
    ├── Total Size
    ├── Per-Model Breakdown
    └── "Open Models Folder" Button
```

### Backend
- `IModelRegistry` - Already implemented
- Download with progress reporting
- SHA256 verification
- Auto-extract ONNX models
- Register in registry

---

## 3. Settings Persistence

### Categories
```
General
├── Language (en/fa/auto)
├── Theme (Dark/Light/System)
├── Start Minimized
├── Check for Updates

Indexing
├── CPU/GPU Backend (Auto/CPU/CUDA)
├── GPU Batch Size
├── CPU Worker Count
├── Max Concurrent Jobs
├── Auto-start Indexing
├── Thumbnail Size
├── Recursive Scan Default
└── Supported Extensions

Search
├── Default Similarity Threshold
├── Max Results
├── Enable OCR Search
├── Enable Face Search
├── Result Sorting (Relevance/Date/Name)
└── Result View (Grid/List/Details)

Privacy
├── Telemetry (Disabled)
├── Crash Reports (Disabled)
├── Clear Search History
└── Clear Thumbnail Cache

Advanced
├── Log Level (Debug/Info/Warning/Error)
├── Max Log Files
├── Database Path
├── Thumbnail Folder
├── Models Folder
└── Reset to Defaults
```

### Storage
- SQLite `Settings` table (already exists)
- JSON export/import for backup
- Migration on version upgrade

---

## 4. Documentation

### User Guide (UserGuide.md)
```
# PhotoAI User Guide

## Getting Started
1. Add folders to index
2. Wait for indexing
3. Search naturally

## Search
- Natural language: "red car near beach"
- By person: "John at beach"
- By object: "dog in park"
- By image: drag & drop
- Filters: date, camera, location

## People
- Face detection runs automatically
- Name people, add reference photos
- Merge/split people
- Browse by person

## Map View
- GPS clustering
- Zoom to cluster
- Filter by location

## Smart Albums
- Auto-created: People, Animals, Nature, Travel, etc.
- Update automatically

## Video Search
- Search video content
- Jump to timestamp

## Settings
- CPU/GPU processing
- Model management
- Privacy settings

## Troubleshooting
- Indexing stuck
- Out of memory
- Missing models
```

### Developer Guide (DeveloperGuide.md)
```
# PhotoAI Developer Guide

## Architecture
- Clean Architecture layers
- Dependency Injection
- Repository Pattern
- ONNX Runtime Abstraction

## Adding New AI Models
1. Implement interface (IImageEmbeddingModel, etc.)
2. Register in ServiceCollectionExtensions
3. Add to ModelRegistry
4. Update ModelManager

## Extending Search
1. Add new signal to SearchRanker
2. Update RankingWeights
3. Add filter to HybridSearchService

## Testing
- Unit tests: dotnet test
- Integration tests: PhotoAI.Indexing.Tests
- Benchmarks: BenchmarkDotNet
```

### API Reference (API.md)
- All public interfaces
- Repository methods
- AI model interfaces
- Search service

---

## 5. Benchmarking

### Benchmark Categories
```
Inference (ms/image)
├── SigLIP Image Embedding (CPU/GPU)
├── SigLIP Text Embedding (CPU/GPU)
├── SCRFD Face Detection (CPU/GPU)
├── SFace Face Embedding (CPU/GPU)
├── RTDETRv2 Object Detection (CPU/GPU)
├── Places365 Scene Classification (CPU/GPU)
├── PaddleOCR Detection (CPU/GPU)
├── PaddleOCR Recognition (CPU/GPU)
└── Moondream Captioning (CPU/GPU)

Indexing (images/sec)
├── Metadata Only
├── + Thumbnails
├── + Face Detection
├── + Face Embeddings
├── + Object Detection
├── + Scene Classification
├── + OCR
├── + Video Frames
└── Full Pipeline

Search (ms/query)
├── Text Search
├── Image Search
├── Person Search
├── Object Search
├── Hybrid Search
└── Video Search

Storage
├── DB Size per 100K images
├── Vector Index Size
├── Thumbnail Size
└── OCR Index Size
```

### Hardware Test Matrix
```
Hardware                          | CPU Only | CUDA (4060 Ti 16GB)
----------------------------------|----------|-------------------
Intel i7-13th + RTX 4060 Ti 16GB  |    ✓     |        ✓
Intel i5-12th (no GPU)            |    ✓     |        -
AMD Ryzen 7 + RTX 3080            |    ✓     |        ✓
Apple M2 (via Rosetta)            |    ✓     |        -
```

### BenchmarkDotNet Project
```
PhotoAI.Benchmarks/
├── InferenceBenchmarks.cs
├── IndexingBenchmarks.cs
├── SearchBenchmarks.cs
├── StorageBenchmarks.cs
└── Program.cs (BenchmarkRunner)
```

### Reporting
```markdown
# Benchmark Results - PhotoAI v1.0.0
Date: 2025-01-15
Hardware: i7-13700K + RTX 4060 Ti 16GB / 32GB DDR5
OS: Windows 11 23H2

## Inference (RTX 4060 Ti 16GB, batch=32)
| Model              | Time (ms) | Throughput (img/s) |
|--------------------|-----------|-------------------|
| SigLIP Image       | 12.3      | 81                |
| SigLIP Text        | 3.2       | 312               |
| SCRFD Face         | 8.7       | 115               |
| SFace              | 4.1       | 244               |
| RTDETRv2           | 23.5      | 42                |
| Places365          | 5.8       | 172               |
| PaddleOCR Det      | 15.2      | 66                |
| PaddleOCR Rec      | 3.8       | 263               |

## Indexing (100K images)
| Stage                | Time | Rate (img/s) |
|----------------------|------|-------------|
| Metadata + Thumbs    | 45s  | 2,222       |
| + Face Detection     | 180s | 555         |
| + Face Embeddings    | 90s  | 1,111       |
| + Object Detection   | 120s | 833         |
| + Scene Classification| 60s | 1,666       |
| + OCR                | 300s | 333         |
| **Total**            | **16m** | **1,041**   |

## Search
| Query Type     | Latency (ms) | Results |
|----------------|--------------|---------|
| Text "red car" | 45           | 50      |
| Image Similar  | 38           | 50      |
| Person "John"  | 32           | 25      |
| Hybrid         | 52           | 50      |
```

---

## Implementation Checklist

### Installer (MSIX)
- [ ] PhotoAI.Packaging project
- [ ] Package.appxmanifest
- [ ] Assets (logos at all scales)
- [ ] AppInstaller for updates
- [ ] Code signing script
- [ ] GitHub Actions for CI/CD

### Model Management UI
- [ ] Models page in Settings
- [ ] Model list with virtualization
- [ ] Install from URL/file
- [ ] Progress dialog
- [ ] License display
- [ ] Remove/disable actions

### Settings
- [ ] Settings page categories
- [ ] Export/Import settings
- [ ] Reset to defaults
- [ ] Auto-save on change

### Documentation
- [ ] UserGuide.md
- [ ] DeveloperGuide.md
- [ ] API.md
- [ ] README.md update

### Benchmarking
- [ ] BenchmarkDotNet project
- [ ] Inference benchmarks
- [ ] Indexing benchmarks
- [ ] Search benchmarks
- [ ] CI integration
- [ ] Results reporting

---

## Commands

```bash
# Build all
dotnet build PhotoAI.sln -c Release

# Run tests
dotnet test PhotoAI.sln -c Release

# Run benchmarks
dotnet run -c Release -p PhotoAI.Benchmarks/PhotoAI.Benchmarks.csproj

# Build MSIX
dotnet msbuild PhotoAI.Packaging/PhotoAI.Packaging.csproj -t:Build -p:Configuration=Release -p:Platform=x64

# Run app
dotnet run -c Release -p src/PhotoAI.App/PhotoAI.App.csproj
```