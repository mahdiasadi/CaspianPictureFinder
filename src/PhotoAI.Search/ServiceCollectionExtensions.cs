using Microsoft.Extensions.DependencyInjection;
using PhotoAI.Core.Interfaces;
using PhotoAI.Search;

namespace PhotoAI.Search;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhotoAiSearch(this IServiceCollection services)
    {
        services.AddScoped<SearchRanker>();
        services.AddScoped<SimilarImageSearch>();
        services.AddScoped<TextSearch>();
        services.AddScoped<HybridSearchService>();
        return services;
    }
}