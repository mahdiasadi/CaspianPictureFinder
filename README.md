# CaspianPictureFinder / یافتن عکس کاسپین

**Offline AI-Powered Photo & Video Search for Windows**  
**جستجوی هوشمند عکس و ویدئو با هوش مصنوعی - کاملاً آفلاین**

---

## 📖 Overview / نمای کلی

CaspianPictureFinder is a production-quality Windows desktop application for **offline AI-powered photo and video management/search**. It competes with products like Excire Foto, digiKam, PhotoPrism, and Google Photos while remaining **100% local/offline**.

CaspianPictureFinder یک برنامه دسکتاپ ویندوز با کیفیت تولیدی برای **مدیریت و جستجوی عکس و ویدئو با هوش مصنوعی به صورت آفلاین** است. این برنامه با محصولاتی مانند Excire Foto، digiKam، PhotoPrism و Google Photos رقابت می‌کند در حالی که **۱۰۰٪ محلی و آفلاین** باقی می‌ماند.

---

## ✨ Key Features / ویژگی‌های اصلی

### 🔍 Search Capabilities / قابلیت‌های جستجو
| Feature / ویژگی | Description / توضیح |
|----------------|-------------------|
| **Natural Language Search** / جستجوی زبان طبیعی | "red car near building", "people at beach", "dog in park" |
| **Search by Image** / جستجو با عکس | Similar image search, visual similarity / جستجوی عکس‌های مشابه، شباهت بصری |
| **Face Recognition** / تشخیص چهره | Detect, recognize, group people / تشخیص، شناسایی، گروه‌بندی افراد |
| **Object Detection** / تشخیص شیء | Cars, dogs, phones, buildings, etc. / ماشین، سگ، گوشی، ساختمان و غیره |
| **Scene Classification** / طبقه‌بندی صحنه | Beach, mountain, city, forest, sunset, etc. / ساحل، کوه، شهر، جنگل، غروب و غیره |
| **Video Semantic Search** / جستجوی معنایی ویدئو | "people playing football at 02:14" / "افراد بازی فوتبال در ۰۲:۱۴" |
| **OCR Search** / جستجوی OCR | Search text in images / جستجوی متن در عکس‌ها |
| **Metadata Search** / جستجوی متادیتا | EXIF, camera, GPS, date, location / اکسیف، دوربین، جی‌پی‌اس، تاریخ، مکان |
| **Hybrid Search** / جستجوی ترکیبی | Combine semantic + face + object + scene + metadata / ترکیب معنایی + چهره + شیء + صحنه + متادیتا |

### 👥 People Management / مدیریت افراد
- **People Browser** / مرورگر افراد: Grid with face thumbnails, photo counts, dates
- **Multiple Reference Images** / تصاویر مرجع متعدد: Add multiple photos per person
- **Face Clustering** / خوشه‌بندی چهره: Auto-group unknown faces
- **Merge/Split People** / ادغام/تفکیک افراد: Correct wrong assignments

### 🎬 Video Support / پشتیبانی ویدئو
- **Formats**: MP4, MOV, AVI, MKV, WebM
- **Smart Frame Sampling** / نمونه‌گیری هوشمند فریم: Scene change detection, keyframe extraction
- **Timestamp Results** / نتایج با تایم‌استمپ: Click to open video at matching moment

### 🔄 Duplicate Detection / تشخیص تکراری
- **Exact Duplicates** / تکراری‌های دقیق: SHA-256 content hash
- **Near Duplicates** / تکراری‌های نزدیک: pHash, dHash, perceptual similarity
- **Duplicate Browser** / مرورگر تکراری: Compare, keep best quality, select/delete

### ⚙️ Technical Architecture / معماری فنی
| Component / جزء | Technology / تکنولوژی |
|----------------|---------------------|
| **UI Framework** / فریم‌ورک UI | WPF + MVVM |
| **Database** / پایگاه داده | SQLite (EF Core) |
| **Vector Search** / جستجوی برداری | Local HNSW + Optional Qdrant |
| **AI Runtime** / محیط اجرای AI | ONNX Runtime (CPU + CUDA) |
| **Video Processing** / پردازش ویدئو | FFmpeg |
| **Architecture** / معماری | Clean Architecture, DI, Modular |

### 🖥️ Hardware Support / پشتیبانی سخت‌افزاری
- **CPU**: ONNX Runtime CPU (all machines)
- **GPU**: NVIDIA CUDA (RTX 4060 Ti 16GB optimized)
- **Auto-detection**: CPU cores, RAM, VRAM, CUDA availability
- **User Choice**: Auto / CPU / GPU with batch size control

---

## 🏗️ Project Structure / ساختار پروژه

```
PhotoAI.sln
├── PhotoAI.App              # WPF UI Application
├── PhotoAI.Core             # Domain Models, Interfaces
├── PhotoAI.Application      # Use Cases, Services
├── PhotoAI.Infrastructure   # SQLite, File System, Config
├── PhotoAI.AI               # ONNX, Model Management, Inference
├── PhotoAI.AI.Face          # Face Detection, Embeddings
├── PhotoAI.AI.Vision        # CLIP, Image Embeddings, Object/Scene
├── PhotoAI.AI.OCR           # OCR Engine
├── PhotoAI.Media            # Image/Video Processing, FFmpeg
├── PhotoAI.Search           # Hybrid Search, Ranking, Filters
├── PhotoAI.Vector           # Local HNSW, Qdrant
├── PhotoAI.Indexing         # Scan Pipeline, Background Workers
├── PhotoAI.Storage          # SQLite, Cache
├── PhotoAI.Benchmarks       # Performance Benchmarks
├── PhotoAI.Packaging        # Installer, Packaging
└── tests/                   # Unit & Integration Tests
```

---

## 🚀 Development Phases / مراحل توسعه

| Phase / فاز | Focus / تمرکز | Status / وضعیت |
|------------|--------------|---------------|
| **Phase 1** | WPF, SQLite, Scanner, Thumbnails, Exact/pHash Duplicates, Photo Grid | 🔄 In Progress |
| **Phase 2** | ONNX Runtime, CPU/CUDA, Model Manager, Embeddings, Vector Search | ⏳ Planned |
| **Phase 3** | Natural Language Search, Hybrid Ranking, Search Filters | ⏳ Planned |
| **Phase 4** | Face Detection, Embeddings, People Browser, Face Search | ⏳ Planned |
| **Phase 5** | Object Detection, Scene Classification, Smart Albums | ⏳ Planned |
| **Phase 6** | OCR, Image Captioning, Document Detection | ⏳ Planned |
| **Phase 7** | Video Indexing, Keyframes, Timestamp Search | ⏳ Planned |
| **Phase 8** | Qdrant Integration, Advanced Ranking, Large Library Optimization | ⏳ Planned |
| **Phase 9** | Installer, Model Management UI, Settings, Documentation | ⏳ Planned |

---

## 🤖 AI Models / مدل‌های هوش مصنوعی

| Task / وظیفه | Model / مدل | License / لایسنس | Size / اندازه | CPU | CUDA |
|-------------|------------|------------------|-------------|-----|------|
| Image Embedding | CLIP ViT-B/32 (ONNX) | Apache 2.0 | ~150 MB | ✅ | ✅ |
| Face Detection | RetinaFace / SCRFD | Apache 2.0 / MIT | ~20 MB | ✅ | ✅ |
| Face Embedding | ArcFace / AdaFace | MIT / Custom | ~50 MB | ✅ | ✅ |
| Object Detection | YOLOv8n / YOLOv10n | AGPL-3.0 / Ultralytics | ~6 MB | ✅ | ✅ |
| Scene Classification | Places365 ResNet50 | BSD-2 | ~100 MB | ✅ | ✅ |
| OCR | PaddleOCR / Tesseract | Apache 2.0 | ~50 MB | ✅ | ✅ |

> See [MODEL-LICENSE.md](MODEL-LICENSE.md) for detailed license analysis.

---

## 📋 Requirements / پیش‌نیازها

- **OS**: Windows 10/11 (64-bit)
- **.NET**: .NET 8 SDK or newer
- **RAM**: 8 GB minimum (16 GB+ recommended for large libraries)
- **GPU**: NVIDIA GPU with 4 GB+ VRAM for CUDA acceleration (optional)
- **Storage**: SSD recommended for index/database

---

## 🛠️ Building / ساخت پروژه

```bash
# Clone
git clone https://github.com/yourusername/CaspianPictureFinder.git
cd CaspianPictureFinder

# Restore & Build
dotnet restore
dotnet build --configuration Release

# Run
dotnet run --project src/PhotoAI.App/PhotoAI.App.csproj
```

---

## 📚 Documentation / مستندات

| Document / سند | Description / توضیح |
|---------------|-------------------|
| [ARCHITECTURE.md](ARCHITECTURE.md) | System architecture & design decisions |
| [DeveloperGuide.md](DeveloperGuide.md) | Development setup, coding standards, workflows |
| [UserGuide.md](UserGuide.md) | End-user guide (Persian/English) |
| [MODEL-LICENSE.md](MODEL-LICENSE.md) | AI model license analysis |
| [PHASE9.md](PHASE9.md) | Phase 9 implementation details |

---

## 🔒 Privacy / حریم خصوصی

**100% Offline - No Cloud / کاملاً آفلاین - بدون ابر**

- ✅ No telemetry / بدون телеметری
- ✅ No analytics / بدون آنالیتیکس
- ✅ No external API calls / بدون تماس API خارجی
- ✅ No automatic upload / بدون آپلود خودکار
- ✅ Photos never leave your computer / عکس‌ها از کامپیوتر شما خارج نمی‌شوند

---

## 📄 License / لایسنس

MIT License - See [LICENSE](LICENSE) for details.

---

## 🤝 Contributing / مشارکت

Contributions welcome! Please read the [Developer Guide](DeveloperGuide.md) first.

---

## 🌟 Star History / تاریخ ستاره‌ها

If you find this project useful, please consider giving it a star!  
اگر این پروژه برای شما مفید بود، لطفاً ستاره دهید!

---

**Made with ❤️ for Windows users who value privacy and local AI**  
**ساخته شده با ❤️ برای کاربران ویندوز که حریم خصوصی و هوش مصنوعی محلی را مهم می‌دانند**