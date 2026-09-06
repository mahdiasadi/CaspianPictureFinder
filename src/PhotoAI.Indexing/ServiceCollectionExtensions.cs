using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhotoAI.Core.Interfaces;
using PhotoAI.Indexing.Pipeline;

namespace PhotoAI.Indexing;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhotoAiIndexing(this IServiceCollection services, string thumbnailDirectory)
    {
        services.AddSingleton<FileScanner>();
        services.AddSingleton<MetadataWorker>();
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ThumbnailWorker>>();
            var processor = sp.GetRequiredService<IImageProcessor>();
            return new ThumbnailWorker(logger, processor, thumbnailDirectory);
        });
        services.AddSingleton<FaceDetectionWorker>();
        services.AddSingleton<ObjectDetectionWorker>();
        services.AddSingleton<SceneClassificationWorker>();
        services.AddSingleton<VideoFrameEmbeddingWorker>();
        services.AddSingleton<SmartAlbumWorker>();
        services.AddSingleton<MapViewWorker>();
        services.AddSingleton<DuplicateFinder>();
        services.AddSingleton<IndexingPipeline>();

        return services;
    }
}
