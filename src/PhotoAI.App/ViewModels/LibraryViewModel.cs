using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.App.ViewModels;

public partial class LibraryViewModel : ObservableObject
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IFolderRepository _folderRepository;
    private readonly ILogger<LibraryViewModel> _logger;

    [ObservableProperty] private ObservableCollection<MediaItemViewModel> _mediaItems = new();
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _imageCount;
    [ObservableProperty] private int _videoCount;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private MediaItemViewModel? _selectedItem;

    private int _currentPage;
    private const int PageSize = 100;

    public LibraryViewModel(
        IMediaRepository mediaRepository,
        IFolderRepository folderRepository,
        ILogger<LibraryViewModel> logger)
    {
        _mediaRepository = mediaRepository;
        _folderRepository = folderRepository;
        _logger = logger;

        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            TotalCount = (int)await _mediaRepository.GetCountAsync();
            ImageCount = (int)await _mediaRepository.GetCountByStatusAsync(MediaStatus.Indexed);
            _currentPage = 0;
            MediaItems.Clear();
            await LoadPageAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (IsLoading) return;
        await LoadPageAsync();
    }

    [RelayCommand]
    private async Task RemoveFolderAsync(Folder folder)
    {
        await _folderRepository.DeleteAsync(folder.Id);
        await LoadDataAsync();
    }

    private async Task LoadPageAsync()
    {
        try
        {
            IsLoading = true;
            var items = await _mediaRepository.GetAllAsync(_currentPage * PageSize, PageSize);

            foreach (var item in items)
            {
                MediaItems.Add(new MediaItemViewModel(item));
            }

            _currentPage++;
            StatusMessage = $"Showing {MediaItems.Count} of {TotalCount} items";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load library");
            StatusMessage = "Failed to load library";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public partial class MediaItemViewModel : ObservableObject
{
    private readonly MediaItem _mediaItem;

    public long Id => _mediaItem.Id;
    public string FileName => _mediaItem.FileName;
    public string FilePath => _mediaItem.FilePath;
    public string Extension => _mediaItem.Extension;
    public long FileSize => _mediaItem.FileSize;
    public MediaType MediaType => _mediaItem.MediaType;
    public int? Width => _mediaItem.Width;
    public int? Height => _mediaItem.Height;
    public DateTime? DateTaken => _mediaItem.DateTaken;
    public DateTime DateModified => _mediaItem.DateModified;
    public string? CameraMake => _mediaItem.CameraMake;
    public string? CameraModel => _mediaItem.CameraModel;
    public string? ThumbnailPath => _mediaItem.ThumbnailPath;

    public string FileSizeText => FormatFileSize(_mediaItem.FileSize);
    public string ResolutionText => _mediaItem.Width.HasValue && _mediaItem.Height.HasValue
        ? $"{_mediaItem.Width}x{_mediaItem.Height}"
        : "Unknown";
    public string DateText => (_mediaItem.DateTaken ?? _mediaItem.DateModified).ToString("yyyy-MM-dd HH:mm");

    [ObservableProperty] private BitmapImage? _thumbnail;

    public MediaItemViewModel(MediaItem mediaItem)
    {
        _mediaItem = mediaItem;
        _ = LoadThumbnailAsync();
    }

    private async Task LoadThumbnailAsync()
    {
        if (string.IsNullOrEmpty(ThumbnailPath) || !File.Exists(ThumbnailPath))
            return;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(ThumbnailPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 256;
            bitmap.EndInit();
            bitmap.Freeze();
            Thumbnail = bitmap;
        }
        catch
        {
            // Thumbnail load failure is non-fatal
        }
    }

    private static string FormatFileSize(long bytes)
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
