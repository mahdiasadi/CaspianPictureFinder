namespace PhotoAI.Core.Interfaces;

public interface IVideoProcessor
{
    Task<IReadOnlyList<(double Timestamp, byte[] FrameData)>> ExtractKeyframesAsync(string videoPath, int maxFrames = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(double Timestamp, byte[] FrameData)>> ExtractFramesAtIntervalAsync(string videoPath, double intervalSeconds, CancellationToken cancellationToken = default);
    Task<(double DurationSeconds, int Width, int Height)> GetVideoInfoAsync(string videoPath, CancellationToken cancellationToken = default);
    Task<byte[]> ExtractFrameAtAsync(string videoPath, double timestampSeconds, CancellationToken cancellationToken = default);
    bool IsSupportedVideoFile(string filePath);
}
