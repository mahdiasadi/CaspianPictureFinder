using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhotoAI.Core.Models;
using PhotoAI.Indexing.Pipeline;

namespace PhotoAI.App.ViewModels;

public partial class MapViewModel : ObservableObject
{
    private readonly MapViewWorker _mapViewWorker;
    private readonly ILogger<MapViewModel> _logger;

    [ObservableProperty] private ObservableCollection<GpsClusterViewModel> _clusters = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private double _zoomLevel = 2;
    [ObservableProperty] private double _centerLat = 0;
    [ObservableProperty] private double _centerLng = 0;
    [ObservableProperty] private int _totalPhotos = 0;

    public MapViewModel(MapViewWorker mapViewWorker, ILogger<MapViewModel> logger)
    {
        _mapViewWorker = mapViewWorker;
        _logger = logger;
        _ = LoadMapAsync();
    }

    [RelayCommand]
    private async Task LoadMapAsync()
    {
        IsLoading = true;
        try
        {
            var clusters = await _mapViewWorker.GetGpsClustersAsync(_zoomLevel);
            
            Clusters.Clear();
            foreach (var cluster in clusters)
            {
                Clusters.Add(new GpsClusterViewModel(cluster));
            }

            _totalPhotos = clusters.Sum(c => c.Count);
            StatusMessage = $"{_totalPhotos} photos on map, {Clusters.Count} clusters";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load map: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ZoomInAsync()
    {
        ZoomLevel = Math.Min(18, ZoomLevel + 1);
        await LoadMapAsync();
    }

    [RelayCommand]
    private async Task ZoomOutAsync()
    {
        ZoomLevel = Math.Max(0, ZoomLevel - 1);
        await LoadMapAsync();
    }

    [RelayCommand]
    private async Task CenterOnLocationAsync((double Lat, double Lng) location)
    {
        CenterLat = location.Lat;
        CenterLng = location.Lng;
        await LoadMapAsync();
    }
}

public partial class GpsClusterViewModel : ObservableObject
{
    private readonly GpsCluster _cluster;

    public double CenterLat => _cluster.CenterLat;
    public double CenterLng => _cluster.CenterLng;
    public int Count => _cluster.Count;
    public GpsBounds Bounds => _cluster.Bounds;
    public IReadOnlyList<GpsClusterItemViewModel> Items { get; }

    public GpsClusterViewModel(GpsCluster cluster)
    {
        _cluster = cluster;
        Items = cluster.Items.Select(i => new GpsClusterItemViewModel(i)).ToList().AsReadOnly();
    }
}

public partial class GpsClusterItemViewModel : ObservableObject
{
    private readonly GpsClusterItem _item;

    public long MediaItemId => _item.MediaItemId;
    public string FileName => _item.FileName;
    public string? ThumbnailPath => _item.ThumbnailPath;
    public DateTime? DateTaken => _item.DateTaken;
    public double Lat => _item.Lat;
    public double Lng => _item.Lng;

    public GpsClusterItemViewModel(GpsClusterItem item)
    {
        _item = item;
    }
}