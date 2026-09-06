using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Indexing.Pipeline;

public class FileScanner
{
    private readonly ILogger<FileScanner> _logger;
    private readonly IImageProcessor _imageProcessor;

    public FileScanner(ILogger<FileScanner> logger, IImageProcessor imageProcessor)
    {
        _logger = logger;
        _imageProcessor = imageProcessor;
    }

    public async Task ScanDirectoryAsync(
        string directoryPath,
        ChannelWriter<MediaItemInfo> writer,
        bool recursive = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.EnumerateFiles(directoryPath, "*.*", searchOption);

            foreach (var filePath in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_imageProcessor.IsSupportedImageFile(filePath) || _imageProcessor.IsSupportedVideoFile(filePath))
                {
                    try
                    {
                        var fileInfo = new FileInfo(filePath);

                        // Security: validate path
                        var fullPath = Path.GetFullPath(filePath);
                        if (fullPath != filePath)
                        {
                            _logger.LogWarning("Path traversal detected, skipping: {FilePath}", filePath);
                            continue;
                        }

                        // Skip zero-length files
                        if (fileInfo.Length == 0)
                        {
                            _logger.LogDebug("Skipping empty file: {FilePath}", filePath);
                            continue;
                        }

                        var info = new MediaItemInfo
                        {
                            FilePath = fullPath,
                            FileName = fileInfo.Name,
                            Extension = fileInfo.Extension,
                            FileSize = fileInfo.Length,
                            DateModified = fileInfo.LastWriteTimeUtc,
                            MediaType = _imageProcessor.IsSupportedImageFile(filePath) ? MediaType.Image : MediaType.Video
                        };

                        await writer.WriteAsync(info, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error reading file info: {FilePath}", filePath);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning directory: {DirectoryPath}", directoryPath);
        }
    }
}
