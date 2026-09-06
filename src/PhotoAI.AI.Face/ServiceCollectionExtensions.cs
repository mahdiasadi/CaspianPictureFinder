using Microsoft.Extensions.DependencyInjection;
using PhotoAI.AI.Face;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Inference;

namespace PhotoAI.AI.Face;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhotoAiFace(this IServiceCollection services)
    {
        services.AddTransient<IFaceDetector>(sp => new ScrfdFaceDetector(sp.GetRequiredService<IInferenceEngine>()));
        services.AddTransient<IFaceEmbeddingModel>(sp => new SFaceEmbeddingModel(sp.GetRequiredService<IInferenceEngine>()));
        return services;
    }
}