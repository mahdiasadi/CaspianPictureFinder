using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhotoAI.Core.Interfaces;
using PhotoAI.Storage.Data;
using PhotoAI.Storage.Repositories;

namespace PhotoAI.Storage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhotoAiStorage(this IServiceCollection services, string dbPath)
    {
        services.AddSingleton<Func<PhotoAiDbContext>>(() => new PhotoAiDbContext(dbPath));
        services.AddSingleton<PhotoAiDbContext>(sp =>
        {
            var factory = sp.GetRequiredService<Func<PhotoAiDbContext>>();
            return factory();
        });

        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<IFolderRepository, FolderRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IFaceRepository, FaceRepository>();
        services.AddScoped<IObjectDetectionRepository, ObjectDetectionRepository>();
        services.AddScoped<ISceneRepository, SceneRepository>();
        services.AddScoped<IEmbeddingRepository, EmbeddingRepository>();
        services.AddScoped<IDuplicateRepository, DuplicateRepository>();
        services.AddScoped<IIndexJobRepository, IndexJobRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IAlbumRepository, AlbumRepository>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(string dbPath)
    {
        var context = new PhotoAiDbContext(dbPath);
        await context.Database.EnsureCreatedAsync();
    }
}
