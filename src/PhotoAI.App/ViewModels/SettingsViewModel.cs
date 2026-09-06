using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhotoAI.Core.Interfaces;

namespace PhotoAI.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IModelRegistry? _modelRegistry;
    private readonly ILogger<SettingsViewModel> _logger;

    // General
    [ObservableProperty] private string _language = "auto";
    [ObservableProperty] private string _theme = "Dark";
    [ObservableProperty] private bool _startMinimized;
    [ObservableProperty] private bool _checkForUpdates = true;

    // Indexing
    [ObservableProperty] private string _processingBackend = "Auto";
    [ObservableProperty] private int _gpuBatchSize = 32;
    [ObservableProperty] private int _cpuWorkerCount = 4;
    [ObservableProperty] private int _maxConcurrentJobs = 2;
    [ObservableProperty] private bool _autoStartIndexing;
    [ObservableProperty] private int _thumbnailSize = 256;
    [ObservableProperty] private bool _recursiveScanDefault = true;

    // Search
    [ObservableProperty] private float _similarityThreshold = 0.7f;
    [ObservableProperty] private int _maxResults = 100;
    [ObservableProperty] private bool _enableOcrSearch = true;
    [ObservableProperty] private bool _enableFaceSearch = true;
    [ObservableProperty] private string _resultSorting = "Relevance";
    [ObservableProperty] private string _resultView = "Grid";

    // Privacy
    [ObservableProperty] private bool _telemetryEnabled;
    [ObservableProperty] private bool _crashReportsEnabled;

    // Advanced
    [ObservableProperty] private string _logLevel = "Information";
    [ObservableProperty] private int _maxLogFiles = 30;
    [ObservableProperty] private string _databasePath = string.Empty;
    [ObservableProperty] private string _thumbnailFolder = string.Empty;
    [ObservableProperty] private string _modelsFolder = string.Empty;

    [ObservableProperty] private string _statusMessage = string.Empty;

    public string[] Languages { get; } = ["auto", "en", "fa"];
    public string[] Themes { get; } = ["Dark", "Light", "System"];
    public string[] Backends { get; } = ["Auto", "CPU", "GPU (CUDA)"];
    public string[] SortOptions { get; } = ["Relevance", "Date Desc", "Date Asc", "Name", "Size"];
    public string[] ViewOptions { get; } = ["Grid", "List", "Details"];
    public string[] LogLevels { get; } = ["Debug", "Information", "Warning", "Error"];

    public SettingsViewModel(
        ISettingsRepository settingsRepository,
        ILogger<SettingsViewModel> logger,
        IModelRegistry? modelRegistry = null)
    {
        _settingsRepository = settingsRepository;
        _logger = logger;
        _modelRegistry = modelRegistry;
        _ = LoadSettingsAsync();
    }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _settingsRepository.GetAllAsync();
            var dict = (IReadOnlyDictionary<string, string>)settings;

            Language = dict.GetValueOrDefault("Language", "auto");
            Theme = dict.GetValueOrDefault("Theme", "Dark");
            StartMinimized = bool.TryParse(dict.GetValueOrDefault("StartMinimized", "false"), out var sm) && sm;
            CheckForUpdates = bool.TryParse(dict.GetValueOrDefault("CheckForUpdates", "true"), out var cu) && cu;

            ProcessingBackend = dict.GetValueOrDefault("ProcessingBackend", "Auto");
            GpuBatchSize = int.TryParse(dict.GetValueOrDefault("GpuBatchSize", "32"), out var gbs) ? gbs : 32;
            CpuWorkerCount = int.TryParse(dict.GetValueOrDefault("CpuWorkerCount", "4"), out var cw) ? cw : 4;
            MaxConcurrentJobs = int.TryParse(dict.GetValueOrDefault("MaxConcurrentJobs", "2"), out var mc) ? mc : 2;
            AutoStartIndexing = bool.TryParse(dict.GetValueOrDefault("AutoStartIndexing", "false"), out var ai) && ai;
            ThumbnailSize = int.TryParse(dict.GetValueOrDefault("ThumbnailSize", "256"), out var ts) ? ts : 256;
            RecursiveScanDefault = bool.TryParse(dict.GetValueOrDefault("RecursiveScanDefault", "true"), out var rs) && rs;

            SimilarityThreshold = float.TryParse(dict.GetValueOrDefault("SimilarityThreshold", "0.7"), out var st) ? st : 0.7f;
            MaxResults = int.TryParse(dict.GetValueOrDefault("MaxResults", "100"), out var mr) ? mr : 100;
            EnableOcrSearch = bool.TryParse(dict.GetValueOrDefault("EnableOcrSearch", "true"), out var eo) && eo;
            EnableFaceSearch = bool.TryParse(dict.GetValueOrDefault("EnableFaceSearch", "true"), out var ef) && ef;
            ResultSorting = dict.GetValueOrDefault("ResultSorting", "Relevance");
            ResultView = dict.GetValueOrDefault("ResultView", "Grid");

            TelemetryEnabled = bool.TryParse(dict.GetValueOrDefault("TelemetryEnabled", "false"), out var te) && te;
            CrashReportsEnabled = bool.TryParse(dict.GetValueOrDefault("CrashReportsEnabled", "false"), out var cr) && cr;

            LogLevel = dict.GetValueOrDefault("LogLevel", "Information");
            MaxLogFiles = int.TryParse(dict.GetValueOrDefault("MaxLogFiles", "30"), out var mlf) ? mlf : 30;
            DatabasePath = dict.GetValueOrDefault("DatabasePath", string.Empty);
            ThumbnailFolder = dict.GetValueOrDefault("ThumbnailFolder", string.Empty);
            ModelsFolder = dict.GetValueOrDefault("ModelsFolder", string.Empty);

            // Load defaults if empty
            if (string.IsNullOrEmpty(DatabasePath))
                DatabasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhotoAI", "photoai.db");
            if (string.IsNullOrEmpty(ThumbnailFolder))
                ThumbnailFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhotoAI", "thumbnails");
            if (string.IsNullOrEmpty(ModelsFolder))
                ModelsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhotoAI", "models");

            StatusMessage = "Settings loaded";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings");
            StatusMessage = "Failed to load settings";
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        try
        {
            await _settingsRepository.SetAsync("Language", Language);
            await _settingsRepository.SetAsync("Theme", Theme);
            await _settingsRepository.SetAsync("StartMinimized", StartMinimized.ToString());
            await _settingsRepository.SetAsync("CheckForUpdates", CheckForUpdates.ToString());

            await _settingsRepository.SetAsync("ProcessingBackend", ProcessingBackend);
            await _settingsRepository.SetAsync("GpuBatchSize", GpuBatchSize.ToString());
            await _settingsRepository.SetAsync("CpuWorkerCount", CpuWorkerCount.ToString());
            await _settingsRepository.SetAsync("MaxConcurrentJobs", MaxConcurrentJobs.ToString());
            await _settingsRepository.SetAsync("AutoStartIndexing", AutoStartIndexing.ToString());
            await _settingsRepository.SetAsync("ThumbnailSize", ThumbnailSize.ToString());
            await _settingsRepository.SetAsync("RecursiveScanDefault", RecursiveScanDefault.ToString());

            await _settingsRepository.SetAsync("SimilarityThreshold", SimilarityThreshold.ToString());
            await _settingsRepository.SetAsync("MaxResults", MaxResults.ToString());
            await _settingsRepository.SetAsync("EnableOcrSearch", EnableOcrSearch.ToString());
            await _settingsRepository.SetAsync("EnableFaceSearch", EnableFaceSearch.ToString());
            await _settingsRepository.SetAsync("ResultSorting", ResultSorting);
            await _settingsRepository.SetAsync("ResultView", ResultView);

            await _settingsRepository.SetAsync("TelemetryEnabled", TelemetryEnabled.ToString());
            await _settingsRepository.SetAsync("CrashReportsEnabled", CrashReportsEnabled.ToString());

            await _settingsRepository.SetAsync("LogLevel", LogLevel);
            await _settingsRepository.SetAsync("MaxLogFiles", MaxLogFiles.ToString());
            await _settingsRepository.SetAsync("DatabasePath", DatabasePath);
            await _settingsRepository.SetAsync("ThumbnailFolder", ThumbnailFolder);
            await _settingsRepository.SetAsync("ModelsFolder", ModelsFolder);

            StatusMessage = "Settings saved";
            _logger.LogInformation("Settings saved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            StatusMessage = $"Save failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        var result = MessageBox.Show(
            "Reset all settings to defaults?",
            "Confirm Reset",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            var defaultSettings = new Dictionary<string, string>
            {
                ["Language"] = "auto",
                ["Theme"] = "Dark",
                ["StartMinimized"] = "false",
                ["CheckForUpdates"] = "true",
                ["ProcessingBackend"] = "Auto",
                ["GpuBatchSize"] = "32",
                ["CpuWorkerCount"] = "4",
                ["MaxConcurrentJobs"] = "2",
                ["AutoStartIndexing"] = "false",
                ["ThumbnailSize"] = "256",
                ["RecursiveScanDefault"] = "true",
                ["SimilarityThreshold"] = "0.7",
                ["MaxResults"] = "100",
                ["EnableOcrSearch"] = "true",
                ["EnableFaceSearch"] = "true",
                ["ResultSorting"] = "Relevance",
                ["ResultView"] = "Grid",
                ["TelemetryEnabled"] = "false",
                ["CrashReportsEnabled"] = "false",
                ["LogLevel"] = "Information",
                ["MaxLogFiles"] = "30",
                ["DatabasePath"] = string.Empty,
                ["ThumbnailFolder"] = string.Empty,
                ["ModelsFolder"] = string.Empty
            };

            foreach (var kvp in defaultSettings)
            {
                await _settingsRepository.SetAsync(kvp.Key, kvp.Value);
            }

            await LoadSettingsAsync();
            StatusMessage = "Settings reset to defaults";
        }
    }

    [RelayCommand]
    private async Task ExportSettingsAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Settings",
            Filter = "JSON Files|*.json|All Files|*.*",
            FileName = "PhotoAI-Settings.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var settings = await _settingsRepository.GetAllAsync();
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(dialog.FileName, json);
                StatusMessage = "Settings exported";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export settings");
                StatusMessage = $"Export failed: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task ImportSettingsAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Import Settings",
            Filter = "JSON Files|*.json|All Files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var json = await File.ReadAllTextAsync(dialog.FileName);
                var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (settings != null)
                {
                    foreach (var kvp in settings)
                    {
                        await _settingsRepository.SetAsync(kvp.Key, kvp.Value);
                    }
                    await LoadSettingsAsync();
                    StatusMessage = "Settings imported";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import settings");
                StatusMessage = $"Import failed: {ex.Message}";
            }
        }
    }
}