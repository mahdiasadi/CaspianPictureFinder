# PhotoAI - User Guide

## Table of Contents
1. [Getting Started](#getting-started)
2. [Adding Folders](#adding-folders)
3. [Searching](#searching)
4. [People](#people)
5. [Map View](#map-view)
6. [Smart Albums](#smart-albums)
7. [Video Search](#video-search)
8. [Settings](#settings)
9. [Model Management](#model-management)
10. [Troubleshooting](#troubleshooting)

---

## Getting Started

### First Launch
1. Download and install PhotoAI
2. Launch the application
3. Click **"Add Folder"** in the top bar
3. Select folders containing your photos/videos
4. Click **OK** - indexing starts automatically

### What Happens During Indexing
PhotoAI processes your media in stages:
1. **Metadata Extraction** - EXIF, GPS, camera info
2. **Thumbnails** - 256px previews for fast browsing
3. **AI Analysis** (optional, runs in background):
   - Semantic embeddings (SigLIP2)
   - Face detection & recognition
   - Object detection
   - Scene classification
   - OCR text extraction
   - Image captions
   - Video keyframe extraction

---

## Adding Folders

1. Click **"Add Folder"** in the top toolbar
2. Select a folder containing photos/videos
3. PhotoAI will recursively scan all subfolders
4. Supported formats:
   - **Images**: JPG, PNG, GIF, BMP, TIFF, WebP, HEIC
   - **Videos**: MP4, MOV, AVI, MKV, WebM, WMV, FLV, M4V

### Folder Management
- **Enable/Disable** - Toggle folders on/off without removing
- **Remove** - Stop tracking (doesn't delete files)
- **Re-scan** - Force re-index of folder

---

## Searching

### Natural Language Search
Type naturally in the search box:
- `"red car near building"`
- `"people at the beach"`
- `"dog in the park"`
- `"children playing"`
- `"sunset over the sea"`

### Search by Image
1. Click the **Similar** tab
2. Drag & drop an image, or click to browse
3. Results show visually similar images

### Search by Person
1. Go to **People** tab
2. Click a person
3. See all photos containing that person

### Filters
Click the filter dropdown to narrow results:
- **Type**: Images / Videos / All
- **Date Range**: From / To
- **Camera**: Make / Model
- **Lens**
- **ISO Range**
- **Aperture / Shutter Speed**
- **Focal Length**
- **Location** (GPS bounds)
- **File Type** (JPG, RAW, MP4, etc.)

### Search Results
- **Grid View** - Thumbnail grid (default)
- **List View** - File names with details
- **Details View** - Large preview with metadata
- **Sort**: Relevance, Date, Name, Size

---

## People

### How It Works
1. Face detection runs automatically during indexing
2. Faces are grouped by similarity
3. You assign names to groups
2. System learns and improves

### Managing People
- **Name**: Click "Unknown (X)" → Enter name
- **Add Reference Photos**: Select best photos for recognition
- **Merge**: Combine two people (select both → Merge)
- **Split**: Separate incorrectly grouped faces
- **Remove Face**: Right-click face → "Remove from person"

### Person Profile
- Photo count
- First/Last seen dates
- Reference photos
- All photos containing this person

---

## Map View

### Viewing Photos on Map
1. Click **Map** in navigation
2. Photos with GPS appear as clusters
3. Click cluster to expand
4. Click photo to view

### Map Controls
- **Zoom In/Out**: +/- buttons or scroll wheel
- **Cluster Count**: Shows photos per area
- **Click Photo**: Opens in detail view
- **Total Photos**: Counter in toolbar

---

## Smart Albums

PhotoAI automatically creates these albums:
| Album | Criteria |
|-------|----------|
| **People** | Any detected face |
| **Animals** | Dog, cat, bird, etc. |
| **Vehicles** | Car, truck, motorcycle, etc. |
| **Nature** | Forest, beach, mountain, sunset |
| **Travel** | City, landmark, airport |
| **Food** | Food, dishes, drinks |
| **Screenshots** | Detected screenshots |
| **Documents** | Documents, receipts, forms |
| **Portraits** | Single face, close-up |
| **Selfies** | Front-facing, close face |
| **Night** | High ISO, night scenes |
| **Favorites** | Tagged as favorite |

Albums update automatically as new photos are indexed.

---

## Video Search

### How It Works
1. Video keyframes extracted at scene changes
2. Each frame gets AI embeddings
3. Search finds matching frames
4. Click result → Opens video at timestamp

### Supported Formats
- MP4, MOV, AVI, MKV, WebM, WMV, FLV, M4V

### Results Show
- Video thumbnail
- Timestamp (e.g., "02:14")
- Matching description
- Click to open at timestamp

---

## Settings

### General
| Setting | Options | Default |
|---------|---------|---------|
| Language | Auto, English, Persian | Auto |
| Theme | Dark, Light, System | Dark |
| Start Minimized | On/Off | Off |
| Check for Updates | On/Off | On |

### Indexing
| Setting | Options | Default |
|---------|---------|---------|
| Processing Backend | Auto / CPU / GPU | Auto |
| GPU Batch Size | 1-128 | 32 |
| CPU Workers | 1-16 | 4 |
| Max Concurrent Jobs | 1-8 | 2 |
| Auto-start Indexing | On/Off | Off |
| Thumbnail Size | 128-512 | 256 |
| Recursive Scan | On/Off | On |

### Search
| Setting | Options | Default |
|---------|---------|---------|
| Similarity Threshold | 0.1 - 1.0 | 0.7 |
| Max Results | 10-1000 | 100 |
| Enable OCR Search | On/Off | On |
| Enable Face Search | On/Off | On |
| Result Sorting | Relevance/Date/Name/Size | Relevance |
| Result View | Grid/List/Details | Grid |

### Privacy
| Setting | Options | Default |
|---------|---------|---------|
| Telemetry | On/Off | Off |
| Crash Reports | On/Off | Off |

### Advanced
| Setting | Options | Default |
|---------|---------|---------|
| Log Level | Debug/Info/Warning/Error | Information |
| Max Log Files | 1-100 | 30 |
| Database Path | Custom path | Auto |
| Thumbnail Folder | Custom path | Auto |
| Models Folder | Custom path | Auto |

---

## Model Management

### Access
Settings → **Models** tab

### Model Catalog
Shows all available models with:
- Name & Version
- Purpose (Embedding/Face/Object/OCR/Caption)
- License (Apache 2.0, MIT, CC-BY)
- Size
- CPU/GPU support
- Status (Installed/Not Installed)

### Actions
| Action | Description |
|--------|-------------|
| **Install** | Select .onnx file to install |
| **Remove** | Delete model file |
| **Enable/Disable** | Toggle without removing |
| **Update** | Replace with newer version |

### Supported Models (Recommended)
| Model | Purpose | Size | License |
|-------|---------|------|---------|
| SigLIP2 SO400M | Image/Text Embedding | 1.6 GB | Apache 2.0 |
| SCRFD-10G | Face Detection | 17 MB | MIT |
| SFace | Face Recognition | 120 MB | Apache 2.0 |
| RTDETRv2-X | Object Detection | 300 MB | Apache 2.0 |
| Places365 ResNet50 | Scene Classification | 100 MB | CC-BY-4.0 |
| PaddleOCR PP-OCRv5 | OCR | 15 MB | Apache 2.0 |
| Moondream | Image Captioning | 1.7 GB | Apache 2.0 |

### Model Folder
Default: `%LOCALAPPDATA%\PhotoAI\models\`
Each model in its own folder with `metadata.json`

---

## Troubleshooting

### Indexing Stuck / Slow
1. **Check CPU/GPU usage** - Task Manager
2. **Reduce batch size** - Settings → GPU Batch Size
3. **Disable AI features** - Turn off face/object/scene in Settings
4. **Check disk space** - Need 2-3x media size for DB + thumbnails
5. **Check logs** - `%LOCALAPPDATA%\PhotoAI\logs\`

### Out of Memory
- Reduce GPU Batch Size (try 8 or 4)
- Reduce CPU Workers
- Process folders one at a time
- Close other applications

### Missing Models
1. Settings → Models
2. Check if model shows "Not Installed"
3. Click Install → Select .onnx file
4. Download from official releases

### Search Not Working
1. Wait for indexing to complete
2. Check if AI features enabled in Settings
3. Try filename search first
4. Rebuild index: Settings → Rebuild Index

### Video Not Playing
- Install HEVC Video Extensions (Microsoft Store)
- Try different video player association
- Check file isn't corrupted

### Performance Tips
| Issue | Solution |
|-------|----------|
| Slow indexing | Use GPU, increase batch size |
| High RAM usage | Reduce batch size, workers |
| Slow search | Reduce max results, increase threshold |
| High disk usage | Clean thumbnails, reduce size |

### Logs Location
`%LOCALAPPDATA%\PhotoAI\logs\photoai-YYYY-MM-DD.log`

### Reset Everything
1. Close PhotoAI
2. Delete `%LOCALAPPDATA%\PhotoAI\`
3. Restart PhotoAI (fresh start)

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+F` | Focus search |
| `Enter` | Execute search |
| `Esc` | Clear search / Close dialog |
| `Ctrl+Mouse Wheel` | Zoom thumbnails |
| `Arrow Keys` | Navigate grid |
| `Enter` (on item) | Open detail |
| `Delete` | Remove from album |

---

## Privacy

**Your data never leaves this computer.**
- No cloud uploads
- No telemetry (unless enabled)
- No analytics
- All AI runs locally on your hardware
- Database is local SQLite
- Models stored locally

---

## Support

- **GitHub Issues**: [github.com/yourusername/PhotoAI/issues](https://github.com/yourusername/PhotoAI/issues)
- **Documentation**: [github.com/yourusername/PhotoAI/wiki](https://github.com/yourusername/PhotoAI/wiki)
- **Releases**: [github.com/yourusername/PhotoAI/releases](https://github.com/yourusername/PhotoAI/releases)

---

*PhotoAI v1.0 - Local AI Photo Search*
*Privacy-first, offline-first, Windows-native*