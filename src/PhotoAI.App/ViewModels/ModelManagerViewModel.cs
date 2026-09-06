using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.App.ViewModels;

public partial class ModelManagerViewModel : ObservableObject
{
    private readonly IModelRegistry _modelRegistry;
    private readonly ILogger<ModelManagerViewModel> _logger;

    [ObservableProperty] private ObservableCollection<ModelViewModel> _models = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private bool _isProgressVisible;

    public ModelManagerViewModel(IModelRegistry modelRegistry, ILogger<ModelManagerViewModel> logger)
    {
        _modelRegistry = modelRegistry;
        _logger = logger;
        _ = LoadModelsAsync();
    }

    [RelayCommand]
    private async Task LoadModelsAsync()
    {
        IsLoading = true;
        try
        {
            Models.Clear();
            var models = _modelRegistry.GetAllModels();
            foreach (var model in models)
            {
                Models.Add(new ModelViewModel(model));
            }
            StatusMessage = $"{Models.Count} models loaded";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load models");
            StatusMessage = "Failed to load models";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task InstallModelAsync(ModelViewModel modelVm)
    {
        if (modelVm.IsInstalled) return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = $"Select {modelVm.Name} model file (.onnx)",
            Filter = "ONNX Models|*.onnx|All Files|*.*",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };

        if (dialog.ShowDialog() == true)
        {
            IsProgressVisible = true;
            ProgressValue = 0;
            StatusMessage = $"Installing {modelVm.Name}...";

            try
            {
                await _modelRegistry.InstallModelAsync(modelVm.ModelId, dialog.FileName);
                modelVm.IsInstalled = true;
                modelVm.StatusText = "Installed";
                StatusMessage = $"{modelVm.Name} installed successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to install model {ModelId}", modelVm.ModelId);
                StatusMessage = $"Install failed: {ex.Message}";
            }
            finally
            {
                IsProgressVisible = false;
            }
        }
    }

    [RelayCommand]
    private async Task RemoveModelAsync(ModelViewModel modelVm)
    {
        if (!modelVm.IsInstalled) return;

        var result = MessageBox.Show(
            $"Remove {modelVm.Name}? This will delete the model file.",
            "Confirm Remove",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _modelRegistry.RemoveModelAsync(modelVm.ModelId);
                modelVm.IsInstalled = false;
                modelVm.StatusText = "Not Installed";
                StatusMessage = $"{modelVm.Name} removed";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove model {ModelId}", modelVm.ModelId);
                StatusMessage = $"Remove failed: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void ToggleModelAsync(ModelViewModel modelVm)
    {
        if (modelVm.IsEnabled)
        {
            _modelRegistry.DisableModel(modelVm.ModelId);
            modelVm.IsEnabled = false;
            modelVm.StatusText = "Disabled";
        }
        else
        {
            _modelRegistry.EnableModel(modelVm.ModelId);
            modelVm.IsEnabled = true;
            modelVm.StatusText = "Enabled";
        }
        StatusMessage = $"{modelVm.Name} {(modelVm.IsEnabled ? "enabled" : "disabled")}";
    }

    [RelayCommand]
    private void OpenModelsFolder()
    {
        var modelsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PhotoAI", "models");

        if (Directory.Exists(modelsPath))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = modelsPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        else
        {
            MessageBox.Show("Models folder not found", "Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

public partial class ModelViewModel : ObservableObject
{
    private readonly ModelInfo _modelInfo;

    public string ModelId => _modelInfo.ModelId;
    public string Name => _modelInfo.Name;
    public string Version => _modelInfo.Version;
    public string Purpose => _modelInfo.Purpose;
    public string License => _modelInfo.License;
    public string SizeText => FormatSize(_modelInfo.SizeBytes);
    public bool SupportsCpu => _modelInfo.SupportsCpu;
    public bool SupportsCuda => _modelInfo.SupportsCuda;
    public int? EmbeddingDimension => _modelInfo.EmbeddingDimension;

    [ObservableProperty] private bool _isInstalled;
    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private string _statusText = string.Empty;

    public ModelViewModel(ModelInfo modelInfo)
    {
        _modelInfo = modelInfo;
        _isInstalled = modelInfo.IsInstalled;
        _isEnabled = modelInfo.IsEnabled;
        _statusText = modelInfo.IsInstalled ? (modelInfo.IsEnabled ? "Enabled" : "Disabled") : "Not Installed";
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}