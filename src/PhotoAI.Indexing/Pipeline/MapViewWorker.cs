using Microsoft.Extensions.Logging;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Indexing.Pipeline;

public class MapViewWorker
{
    private readonly ILogger<MapViewWorker> _logger;
    private readonly IMediaRepository _mediaRepository;

    public MapViewWorker(ILogger<MapViewWorker> logger, IMediaRepository mediaRepository)
    {
        _logger = logger;
        _mediaRepository = mediaRepository;
    }

    public async Task<List<GpsCluster>> GetGpsClustersAsync(double zoomLevel, CancellationToken cancellationToken = default)
    {
        var mediaItems = await _mediaRepository.GetByStatusAsync(MediaStatus.Indexed, 0, int.MaxValue);
        
        var gpsItems = mediaItems
            .Where(m => m.Latitude.HasValue && m.Longitude.HasValue)
            .Select(m => new
            {
                m.Id,
                m.FileName,
                m.ThumbnailPath,
                m.DateTaken,
                Lat = m.Latitude!.Value,
                Lng = m.Longitude!.Value
            })
            .ToList();

        if (!gpsItems.Any())
            return new List<GpsCluster>();

        // Determine grid size based on zoom level
        double gridSize = GetGridSize(zoomLevel);
        
        var clusters = new Dictionary<(int gridX, int gridY), List<dynamic>>();
        
        foreach (var item in gpsItems)
        {
            int gridX = (int)Math.Floor(item.Lng / gridSize);
            int gridY = (int)Math.Floor(item.Lat / gridSize);
            var key = (gridX, gridY);
            
            if (!clusters.ContainsKey(key))
                clusters[key] = new List<dynamic>();
            
            clusters[key].Add(item);
        }

        var result = new List<GpsCluster>();
        foreach (var kvp in clusters)
        {
            var items = kvp.Value;
            var cluster = new GpsCluster
            {
                CenterLat = items.Average(i => i.Lat),
                CenterLng = items.Average(i => i.Lng),
                Count = items.Count,
                Bounds = new GpsBounds
                {
                    MinLat = items.Min(i => i.Lat),
                    MaxLat = items.Max(i => i.Lat),
                    MinLng = items.Min(i => i.Lng),
                    MaxLng = items.Max(i => i.Lng)
                },
                Items = items.Take(10).Select(i => new GpsClusterItem
                {
                    MediaItemId = i.Id,
                    FileName = i.FileName,
                    ThumbnailPath = i.ThumbnailPath,
                    DateTaken = i.DateTaken,
                    Lat = i.Lat,
                    Lng = i.Lng
                }).ToList()
            };
            result.Add(cluster);
        }

        return result;
    }

    private static double GetGridSize(double zoomLevel)
    {
        // Zoom level 1 = world view (~40000km), each level halves the size
        // Approximate degrees per zoom level
        return 360.0 / Math.Pow(2, zoomLevel + 1);
    }
}

public class GpsCluster
{
    public double CenterLat { get; set; }
    public double CenterLng { get; set; }
    public int Count { get; set; }
    public GpsBounds Bounds { get; set; } = new();
    public List<GpsClusterItem> Items { get; set; } = new();
}

public class GpsBounds
{
    public double MinLat { get; set; }
    public double MaxLat { get; set; }
    public double MinLng { get; set; }
    public double MaxLng { get; set; }
}

public class GpsClusterItem
{
    public long MediaItemId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public DateTime? DateTaken { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
}