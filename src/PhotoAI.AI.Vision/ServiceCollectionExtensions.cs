using Microsoft.Extensions.DependencyInjection;
using PhotoAI.AI.Vision;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Inference;

namespace PhotoAI.AI.Vision;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhotoAiVision(this IServiceCollection services)
    {
        services.AddTransient<IObjectDetector>(sp => new RTDETRv2ObjectDetector(sp.GetRequiredService<IInferenceEngine>()));
        services.AddTransient<ISceneClassifier>(sp => new Places365SceneClassifier(sp.GetRequiredService<IInferenceEngine>()));
        services.AddTransient<IImageCaptioner>(sp => new MoondreamCaptioner(sp.GetRequiredService<IInferenceEngine>()));
        services.AddTransient<IObjectDetector>(sp => new DocumentDetector(sp.GetRequiredService<IInferenceEngine>()));
        return services;
    }
}