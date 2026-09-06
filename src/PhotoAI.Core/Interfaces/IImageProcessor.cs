namespace PhotoAI.Core.Interfaces;

public interface IImageProcessor
{
    Task<byte[]> ResizeAsync(byte[] imageData, int maxWidth, int maxHeight, CancellationToken cancellationToken = default);
    Task<byte[]> GetThumbnailAsync(byte[] imageData, int size, CancellationToken cancellationToken = default);
    Task<byte[]> CropAsync(byte[] imageData, int x, int y, int width, int height, CancellationToken cancellationToken = default);
    Task<byte[]> ConvertToJpegAsync(byte[] imageData, int quality = 85, CancellationToken cancellationToken = default);
    Task<(int Width, int Height)> GetDimensionsAsync(byte[] imageData, CancellationToken cancellationToken = default);
    Task<byte[]> GetImageBytesAsync(string filePath, CancellationToken cancellationToken = default);
    Task<string> ComputeContentHashAsync(byte[] imageData, CancellationToken cancellationToken = default);
    Task<ulong> ComputePerceptualHashAsync(byte[] imageData, CancellationToken cancellationToken = default);
    bool IsSupportedImageFile(string filePath);
    bool IsSupportedVideoFile(string filePath);
}
