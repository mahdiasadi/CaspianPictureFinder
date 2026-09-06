# PhotoAI Architecture

## Solution Structure

```
PhotoAI.sln
├── src/
│   ├── PhotoAI.App/              # WPF Desktop UI (MVVM)
│   ├── PhotoAI.Core/             # Domain models, interfaces, enums
│   ├── PhotoAI.Application/      # Use cases, application services
│   ├── PhotoAI.Infrastructure/   # External service adapters
│   ├── PhotoAI.AI/               # ONNX Runtime inference abstraction
│   ├── PhotoAI.AI.Face/          # Face detection & embedding models
│   ├── PhotoAI.AI.Vision/        # CLIP, object detection, scene classification
│   ├── PhotoAI.AI.OCR/           # OCR engine
│   ├── PhotoAI.Media/            # Image/video processing, metadata extraction
│   ├── PhotoAI.Search/           # Hybrid search ranking engine
│   ├── PhotoAI.Vector/           # Vector store abstraction (HNSW + Qdrant)
│   ├── PhotoAI.Indexing/         # Background indexing pipeline
│   └── PhotoAI.Storage/          # SQLite + EF Core repositories
├── tests/
│   ├── PhotoAI.Core.Tests/
│   ├── PhotoAI.Application.Tests/
│   ├── PhotoAI.Infrastructure.Tests/
│   └── PhotoAI.Indexing.Tests/
├── MODEL-LICENSE.md              # AI model license documentation
└── ARCHITECTURE.md               # This file
```

## Dependency Graph

```
PhotoAI.App (WPF)
├── PhotoAI.Application
│   └── PhotoAI.Core
├── PhotoAI.Core
├── PhotoAI.Indexing
│   ├── PhotoAI.Core
│   ├── PhotoAI.Application
│   ├── PhotoAI.Media
│   │   └── PhotoAI.Core
│   ├── PhotoAI.Storage
│   │   └── PhotoAI.Core
│   └── PhotoAI.Vector
│       └── PhotoAI.Core
├── PhotoAI.Search
│   └── PhotoAI.Core
└── PhotoAI.Storage
    └── PhotoAI.Core
```

## Key Interfaces

| Interface | Purpose | Location |
|-----------|---------|----------|
| `IMediaRepository` | CRUD for media items | PhotoAI.Core |
| `IFolderRepository` | Folder management | PhotoAI.Core |
| `IPersonRepository` | Person profiles | PhotoAI.Core |
| `IFaceRepository` | Face detection results | PhotoAI.Core |
| `IVectorStore` | Vector similarity search | PhotoAI.Core |
| `IImageEmbeddingModel` | Image-to-vector embedding | PhotoAI.Core |
| `ITextEmbeddingModel` | Text-to-vector embedding | PhotoAI.Core |
| `IFaceDetector` | Face detection | PhotoAI.Core |
| `IFaceEmbeddingModel` | Face embedding | PhotoAI.Core |
| `IObjectDetector` | Object detection | PhotoAI.Core |
| `ISceneClassifier` | Scene classification | PhotoAI.Core |
| `IOcrEngine` | OCR text extraction | PhotoAI.Core |
| `IImageProcessor` | Image manipulation & hashing | PhotoAI.Core |
| `IVideoProcessor` | Video frame extraction | PhotoAI.Core |
| `IModelRegistry` | AI model management | PhotoAI.Core |

## Database Schema (SQLite)

### Tables
- **Folders** - Monitored directories
- **MediaItems** - All photos/videos with metadata
- **Persons** - Named people
- **Faces** - Detected faces with bounding boxes
- **FaceEmbeddings** - Face identity vectors
- **Embeddings** - Image/text embeddings
- **ObjectDetections** - Detected objects with bounding boxes
- **SceneLabels** - Scene classifications
- **VideoFrames** - Extracted video frames
- **MediaTags** - Manual and AI tags
- **OcrResults** - Extracted text
- **DuplicateGroups** - Duplicate/near-duplicate groups
- **DuplicateEntries** - Members of each duplicate group
- **IndexJobs** - Indexing job tracking
- **Albums** - Manual and smart albums
- **AlbumMedia** - Album-media associations
- **Settings** - Application settings

## Indexing Pipeline

```
FileScanner → Channel<MediaItemInfo>
    ↓
MetadataWorker → MediaItem (with EXIF, hashes)
    ↓
ThumbnailWorker → Thumbnail cache
    ↓
DuplicateFinder → Duplicate groups
```

Pipeline features:
- Bounded channels with backpressure
- Cancellation support
- Progress reporting
- Resumable after crash
- Incremental (skip already-indexed files)

## Phase 1 Status

### Implemented
- Solution structure (17 projects)
- Core domain models (15 entities)
- Core interfaces (16 interfaces)
- SQLite storage with EF Core (14 repositories)
- Full database schema with indexes
- File scanner with path validation
- Metadata extraction (EXIF, GPS, camera info)
- Thumbnail generation (256px JPEG)
- Content hash (SHA-256) for exact duplicates
- Perceptual hash (average hash) for near-duplicates
- Duplicate detection (exact + near-duplicate)
- Indexing pipeline with Channel<T> backpressure
- WPF UI with dark theme
- Library view with virtualized photo grid
- Search view with text search
- People browser (placeholder)
- Settings view (CPU/GPU selection)
- 15 passing unit tests

### Not Yet Implemented (Phase 2+)
- ONNX Runtime integration
- CLIP/SigLIP image & text embeddings
- Vector similarity search (HNSW)
- Natural language search
- Face detection (SCRFD)
- Face recognition (SFace)
- Object detection (RTDETRv2)
- Scene classification (Places365)
- OCR (PaddleOCR)
- Image captioning (Moondream)
- Video processing (FFmpeg)
- Qdrant integration
- Smart albums
- Map view
- Advanced hybrid search ranking
