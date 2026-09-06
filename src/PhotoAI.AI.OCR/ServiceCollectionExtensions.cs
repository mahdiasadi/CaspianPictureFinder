using Microsoft.Extensions.DependencyInjection;
using PhotoAI.AI.OCR;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Inference;

namespace PhotoAI.AI.OCR;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhotoAiOCR(this IServiceCollection services)
    {
        services.AddTransient<IOcrEngine>(sp => new PaddleOcrEngine(sp.GetRequiredService<IInferenceEngine>()));
        return services;
    }
}