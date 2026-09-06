using Microsoft.Extensions.DependencyInjection;
using PhotoAI.AI.Models;
using PhotoAI.AI.Face;
using PhotoAI.AI.Vision;
using PhotoAI.Core.Inference;
using PhotoAI.Core.Interfaces;

namespace PhotoAI.AI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhotoAiAI(this IServiceCollection services, string modelsDirectory, ProcessingBackend backend = ProcessingBackend.Auto)
    {
        // Inference engines
        services.AddSingleton<IInferenceEngine>(sp => InferenceEngineFactory.Create(backend));
        services.AddSingleton<CpuInferenceEngine>();
        services.AddSingleton<CudaInferenceEngine>();
        services.AddSingleton<AutoInferenceEngine>();

        // Model manager
        services.AddSingleton<IModelRegistry>(sp => new ModelManager(modelsDirectory));

        // Vision models
        services.AddTransient<IImageEmbeddingModel>(sp => new SigLipImageEmbeddingModel(sp.GetRequiredService<IInferenceEngine>()));
        services.AddTransient<ITextEmbeddingModel>(sp => new SigLipTextEmbeddingModel(sp.GetRequiredService<IInferenceEngine>()));
        services.AddTransient<IObjectDetector>(sp => new RTDETRv2ObjectDetector(sp.GetRequiredService<IInferenceEngine>()));
        services.AddTransient<ISceneClassifier>(sp => new Places365SceneClassifier(sp.GetRequiredService<IInferenceEngine>()));

        // Face models
        services.AddTransient<IFaceDetector>(sp => new ScrfdFaceDetector(sp.GetRequiredService<IInferenceEngine>()));
        services.AddTransient<IFaceEmbeddingModel>(sp => new SFaceEmbeddingModel(sp.GetRequiredService<IInferenceEngine>()));

        // Model manager
        services.AddSingleton<IModelRegistry>(sp => new ModelManager(modelsDirectory));

        return services;
    }
}