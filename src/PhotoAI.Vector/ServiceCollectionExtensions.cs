using Microsoft.Extensions.DependencyInjection;
using PhotoAI.Core.Interfaces;

namespace PhotoAI.Vector;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhotoAiVector(this IServiceCollection services, string indexDirectory)
    {
        services.AddSingleton<IVectorStore>(sp => new LocalVectorStore(indexDirectory));
        return services;
    }
}