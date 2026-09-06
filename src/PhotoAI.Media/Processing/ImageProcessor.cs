using System.Security.Cryptography;
using PhotoAI.Core.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PhotoAI.Media.Processing;

public class ImageProcessor : IImageProcessor
{
    private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp", ".heic", ".heif"
    };

    private static readonly HashSet<string> SupportedVideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".avi", ".mkv", ".webm", ".wmv", ".flv", ".m4v"
    };

    public async Task<byte[]> ResizeAsync(byte[] imageData, int maxWidth, int maxHeight, CancellationToken cancellationToken = default)
    {
        using var image = Image.Load<Bgra32>(imageData);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(maxWidth, maxHeight),
            Mode = ResizeMode.Max,
            Compand = true
        }));
        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms, new JpegEncoder { Quality = 85 });
        return ms.ToArray();
    }

    public async Task<byte[]> GetThumbnailAsync(byte[] imageData, int size, CancellationToken cancellationToken = default)
    {
        using var image = Image.Load<Bgra32>(imageData);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(size, size),
            Mode = ResizeMode.Crop,
            Compand = true
        }));
        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms, new JpegEncoder { Quality = 80 });
        return ms.ToArray();
    }

    public async Task<byte[]> CropAsync(byte[] imageData, int x, int y, int width, int height, CancellationToken cancellationToken = default)
    {
        using var image = Image.Load<Bgra32>(imageData);
        image.Mutate(ctx => ctx.Crop(new Rectangle(x, y, width, height)));
        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms, new JpegEncoder { Quality = 90 });
        return ms.ToArray();
    }

    public async Task<byte[]> ConvertToJpegAsync(byte[] imageData, int quality = 85, CancellationToken cancellationToken = default)
    {
        using var image = Image.Load<Bgra32>(imageData);
        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms, new JpegEncoder { Quality = quality });
        return ms.ToArray();
    }

    public async Task<(int Width, int Height)> GetDimensionsAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        using var image = Image.Load<Bgra32>(imageData);
        return (image.Width, image.Height);
    }

    public async Task<byte[]> GetImageBytesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return await File.ReadAllBytesAsync(filePath, cancellationToken);
    }

    public async Task<string> ComputeContentHashAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream(imageData);
        var hash = await SHA256.HashDataAsync(ms, cancellationToken);
        return Convert.ToHexString(hash);
    }

    public async Task<ulong> ComputePerceptualHashAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream(imageData);
        using var image = Image.Load<Bgra32>(ms);

        image.Mutate(x => x.Resize(32, 32).Grayscale());

        var pixels = new float[32 * 32];
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < accessor.Width; x++)
                {
                    pixels[y * 32 + x] = row[x].R;
                }
            }
        });

        float average = pixels.Average();

        ulong hash = 0;
        for (int i = 0; i < 64; i++)
        {
            if (pixels[i] > average)
            {
                hash |= 1UL << i;
            }
        }

        return hash;
    }

    public bool IsSupportedImageFile(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return SupportedImageExtensions.Contains(ext);
    }

    public bool IsSupportedVideoFile(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return SupportedVideoExtensions.Contains(ext);
    }

    public static double ComputeHammingDistance(ulong hash1, ulong hash2)
    {
        ulong xor = hash1 ^ hash2;
        int count = 0;
        while (xor != 0)
        {
            count++;
            xor &= xor - 1;
        }
        return (double)count / 64.0;
    }

    public static double ComputeSimilarity(ulong hash1, ulong hash2)
    {
        return 1.0 - ComputeHammingDistance(hash1, hash2);
    }
}
