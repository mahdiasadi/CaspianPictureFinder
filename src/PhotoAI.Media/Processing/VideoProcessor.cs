using System.Diagnostics;
using Xabe.FFmpeg;
using PhotoAI.Core.Interfaces;

namespace PhotoAI.Media.Processing;

public class VideoProcessor : IVideoProcessor
{
    private static readonly HashSet<string> SupportedVideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".avi", ".mkv", ".webm", ".wmv", ".flv", ".m4v", ".ts", ".mts", ".m2ts"
    };

    public VideoProcessor()
    {
        // FFmpeg will auto-download if not found
        FFmpeg.SetExecutablesPath(Path.Combine(AppContext.BaseDirectory, "ffmpeg"));
    }

    public async Task<IReadOnlyList<(double Timestamp, byte[] FrameData)>> ExtractKeyframesAsync(
        string videoPath, 
        int maxFrames = 10, 
        CancellationToken cancellationToken = default)
    {
        var frames = new List<(double Timestamp, byte[] FrameData)>();

        try
        {
            var mediaInfo = await FFmpeg.GetMediaInfo(videoPath);
            var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
            
            if (videoStream == null)
                return frames;

            double duration = mediaInfo.Duration.TotalSeconds;
            if (duration <= 0)
                return frames;

            // Extract keyframes at regular intervals
            double interval = Math.Max(1.0, duration / (maxFrames + 1));
            
            for (int i = 1; i <= maxFrames; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                double timestamp = interval * i;
                if (timestamp >= duration - 1)
                    break;

                var frameData = await ExtractFrameAtAsync(videoPath, timestamp, cancellationToken);
                if (frameData != null && frameData.Length > 0)
                {
                    frames.Add((timestamp, frameData));
                }
            }
        }
        catch (Exception)
        {
            // Fallback to scene change detection
        }

        return frames;
    }

    public async Task<IReadOnlyList<(double Timestamp, byte[] FrameData)>> ExtractFramesAtIntervalAsync(
        string videoPath, 
        double intervalSeconds, 
        CancellationToken cancellationToken = default)
    {
        var frames = new List<(double Timestamp, byte[] FrameData)>();

        try
        {
            var mediaInfo = await FFmpeg.GetMediaInfo(videoPath);
            double duration = mediaInfo.Duration.TotalSeconds;

            for (double timestamp = intervalSeconds; timestamp < duration - 1; timestamp += intervalSeconds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var frameData = await ExtractFrameAtAsync(videoPath, timestamp, cancellationToken);
                if (frameData != null && frameData.Length > 0)
                {
                    frames.Add((timestamp, frameData));
                }
            }
        }
        catch (Exception)
        {
            // Ignore errors
        }

        return frames;
    }

    public async Task<(double DurationSeconds, int Width, int Height)> GetVideoInfoAsync(
        string videoPath, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaInfo = await FFmpeg.GetMediaInfo(videoPath);
            var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
            
            if (videoStream != null)
            {
                return (mediaInfo.Duration.TotalSeconds, videoStream.Width, videoStream.Height);
            }
        }
        catch (Exception)
        {
            // Ignore
        }
        
        return (0, 0, 0);
    }

    public async Task<byte[]> ExtractFrameAtAsync(
        string videoPath, 
        double timestampSeconds, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var outputPath = Path.Combine(
                Path.GetTempPath(), 
                $"frame_{Guid.NewGuid():N}.jpg");

            var conversion = FFmpeg.Conversions.New()
                .AddParameter($"-ss {timestampSeconds:F3}")
                .AddParameter($"-i \"{videoPath}\"")
                .AddParameter("-vframes 1")
                .AddParameter("-q:v 2")
                .AddParameter($"\"{outputPath}\"");

            await conversion.Start(cancellationToken);

            if (File.Exists(outputPath))
            {
                var data = await File.ReadAllBytesAsync(outputPath, cancellationToken);
                File.Delete(outputPath);
                return data;
            }
        }
        catch (Exception)
        {
            // Ignore
        }

        return null;
    }

    public bool IsSupportedVideoFile(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return SupportedVideoExtensions.Contains(ext);
    }
}