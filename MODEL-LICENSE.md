# AI Model Licenses

## Recommended Models (Commercial-Ready)

| Component | Model | License | Commercial Use | Notes |
|-----------|-------|---------|---------------|-------|
| **Image/Text Embedding** | SigLIP2 ViT-SO400M-16 | Apache 2.0 | ✅ Yes | Best accuracy among permissive models |
| **Face Detection** | SCRFD-10G | MIT | ✅ Yes | Detection only, no identity data |
| **Face Recognition** | SFace | Apache 2.0 | ✅ Yes | Only commercially-licensed face model |
| **Object Detection** | RTDETRv2-x | Apache 2.0 | ✅ Yes | NMS-free, accurate |
| **OCR** | PaddleOCR PP-OCRv5 | Apache 2.0 | ✅ Yes | 15MB total, 100+ languages |
| **Image Captioning** | Moondream | Apache 2.0 | ✅ Yes | Lightweight VLM |
| **Scene Classification** | Places365 ResNet50 | CC-BY-4.0 | ✅ Yes (attribution required) | 365 scene categories |
| **Perceptual Hashing** | CoenM.ImageSharp.ImageHash | MIT | ✅ Yes | pHash, dHash, average hash |
| **Vector Search** | HNSW-Sharp | MIT | ✅ Yes | Native C# HNSW |

## Models NOT Recommended for Commercial Use

| Model | License | Why Not |
|-------|---------|---------|
| ArcFace (InsightFace) | Non-commercial research only | Training data restrictions |
| AdaFace | Non-commercial research only | Training data restrictions |
| BLIP / BLIP-2 | Research-only weights | Salesforce model card restrictions |
| YOLOv8/YOLO11/YOLO26 (Ultralytics) | AGPL-3.0 | Requires source release or paid enterprise license |
| Surya OCR | Modified OpenRAIL-M | Revenue cap restrictions |

## License Notes

- **Apache 2.0**: Permissive, allows commercial use, modification, and distribution
- **MIT**: Permissive, allows commercial use with minimal restrictions
- **CC-BY-4.0**: Allows commercial use but requires attribution
- **AGPL-3.0**: Copyleft — requires source code release if distributed
- **Research-only**: Weights trained on datasets with usage restrictions

## Attribution Requirements

For Places365 (CC-BY-4.0), include in application credits:
> Scene classification model based on Places365 by MIT CSAIL. © MIT CSAIL. Licensed under CC-BY-4.0.
