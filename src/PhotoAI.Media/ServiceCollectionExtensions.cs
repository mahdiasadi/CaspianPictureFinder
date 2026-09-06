using Microsoft.Extensions.DependencyInjection;
using PhotoAI.Core.Interfaces;
using PhotoAI.Media.Processing;

namespace PhotoAI.Media;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhotoAiMedia(this IServiceCollection services)
    {
        services.AddSingleton<IImageProcessor, ImageProcessor>();
        services.AddSingleton<IVideoProcessor, VideoProcessor>();
        return services;
    }
}
