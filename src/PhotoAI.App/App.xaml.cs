using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PhotoAI.AI;
using PhotoAI.App.ViewModels;
using PhotoAI.Core.Interfaces;
using PhotoAI.Indexing;
using PhotoAI.Media;
using PhotoAI.Storage;
using PhotoAI.Storage.Data;
using PhotoAI.Vector;
using Serilog;

namespace PhotoAI.App;

public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PhotoAI");

        Directory.CreateDirectory(appDataPath);

        var dbPath = Path.Combine(appDataPath, "photoai.db");
        var thumbnailDir = Path.Combine(appDataPath, "thumbnails");
        var modelsDir = Path.Combine(appDataPath, "models");

        Directory.CreateDirectory(thumbnailDir);
        Directory.CreateDirectory(modelsDir);

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(Path.Combine(appDataPath, "logs", "photoai-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                services.AddPhotoAiStorage(dbPath);
                services.AddPhotoAiMedia();
                services.AddPhotoAiAI(modelsDir);
                services.AddPhotoAiVector(thumbnailDir);
                services.AddPhotoAiIndexing(thumbnailDir);

                services.AddSingleton(sp => new PhotoAiDbContext(dbPath));
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<LibraryViewModel>();
                services.AddSingleton<SearchViewModel>();
                services.AddSingleton<PeopleViewModel>();
                services.AddSingleton<MapViewModel>();
                services.AddSingleton<SettingsViewModel>();
            })
            .Build();

        // Initialize database
        PhotoAI.Storage.ServiceCollectionExtensions.InitializeDatabaseAsync(dbPath).GetAwaiter().GetResult();

        var mainWindow = new MainWindow
        {
            DataContext = _host.Services.GetRequiredService<MainViewModel>()
        };
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
