using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhotoAI.App.Views;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;
using PhotoAI.Indexing.Pipeline;

namespace PhotoAI.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MainViewModel> _logger;
    private readonly IndexingPipeline _indexingPipeline;
    private readonly IFolderRepository _folderRepository;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private object? _currentView;
    [ObservableProperty] private bool _isIndexing;
    [ObservableProperty] private double _indexingProgress;
    [ObservableProperty] private string _indexingStatusText = string.Empty;
    [ObservableProperty] private string _indexingSpeed = string.Empty;

    public MainViewModel(
        IServiceProvider serviceProvider,
        ILogger<MainViewModel> logger,
        IndexingPipeline indexingPipeline,
        IFolderRepository folderRepository)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _indexingPipeline = indexingPipeline;
        _folderRepository = folderRepository;

        _indexingPipeline.ProgressChanged += OnIndexingProgress;
        _indexingPipeline.IndexingCompleted += OnIndexingCompleted;

        // Default view
        Navigate("Library");
    }

    [RelayCommand]
    private void Navigate(string? viewName)
    {
        CurrentView = viewName switch
        {
            "Library" => _serviceProvider.GetRequiredService<LibraryViewModel>(),
            "People" => _serviceProvider.GetRequiredService<PeopleViewModel>(),
            "Map" => _serviceProvider.GetRequiredService<MapViewModel>(),
            "Search" => _serviceProvider.GetRequiredService<SearchViewModel>(),
            "Settings" => _serviceProvider.GetRequiredService<SettingsViewModel>(),
            _ => CurrentView
        };

        StatusText = $"Viewing: {viewName}";
    }

    [RelayCommand]
    private async Task ExecuteSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;

        var searchViewModel = _serviceProvider.GetRequiredService<SearchViewModel>();
        searchViewModel.SearchText = SearchText;
        CurrentView = searchViewModel;
        await searchViewModel.ExecuteSearchAsync();
    }

    [RelayCommand]
    private async Task AddFolderAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select a folder to index",
            FileName = "Select Folder",
            Filter = "Folders|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            var folderPath = System.IO.Path.GetDirectoryName(dialog.FileName) ?? dialog.FileName;

            if (await _folderRepository.ExistsByPathAsync(folderPath))
            {
                MessageBox.Show("This folder is already being tracked.", "Folder Already Added",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var folder = new Folder
            {
                Path = folderPath,
                Name = System.IO.Path.GetFileName(folderPath),
                IsEnabled = true,
                RecursiveScan = true,
                DateAdded = DateTime.UtcNow
            };

            await _folderRepository.AddAsync(folder);
            StatusText = $"Added folder: {folder.Name}";

            // Start indexing
            IsIndexing = true;
            await _indexingPipeline.StartAsync();
        }
    }

    private void OnIndexingProgress(IndexProgress progress)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var total = progress.TotalFiles > 0 ? progress.TotalFiles : 1;
            IndexingProgress = (double)progress.ProcessedFiles / total * 100;
            IndexingStatusText = $"{progress.CurrentPhase}: {progress.ProcessedFiles}/{progress.TotalFiles} files";
            IndexingSpeed = $"{progress.FilesPerSecond:F1} files/sec";
        });
    }

    private void OnIndexingCompleted()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsIndexing = false;
            StatusText = "Indexing completed";
        });
    }
}
