using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace PhotoAI.Media.Processing;

public class MetadataExtractor
{
    public MediaItemInfo ExtractFromFile(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var info = new MediaItemInfo
        {
            FilePath = filePath,
            FileName = fileInfo.Name,
            Extension = fileInfo.Extension,
            FileSize = fileInfo.Length,
            DateModified = fileInfo.LastWriteTimeUtc,
            MediaType = GetMediaType(fileInfo.Extension)
        };

        try
        {
            ExtractExifData(filePath, info);
        }
        catch
        {
        }

        return info;
    }

    private void ExtractExifData(string filePath, MediaItemInfo info)
    {
        try
        {
            using var image = Image.Load(filePath);

            info.Width = image.Width;
            info.Height = image.Height;

            if (image.Metadata.ExifProfile != null)
            {
                var exif = image.Metadata.ExifProfile;

                if (exif.TryGetValue(ExifTag.DateTimeOriginal, out IExifValue<string>? dateTaken) &&
                    DateTime.TryParse(dateTaken.Value, out var parsed))
                {
                    info.DateTaken = parsed;
                }

                if (exif.TryGetValue(ExifTag.Make, out IExifValue<string>? make))
                    info.CameraMake = make.Value;
                if (exif.TryGetValue(ExifTag.Model, out IExifValue<string>? model))
                    info.CameraModel = model.Value;

                if (exif.TryGetValue(ExifTag.LensModel, out IExifValue<string>? lens))
                    info.Lens = lens.Value;

                if (exif.TryGetValue(ExifTag.ISOSpeedRatings, out IExifValue<ushort[]>? iso) && iso.Value.Length > 0)
                    info.Iso = iso.Value[0];

                if (exif.TryGetValue(ExifTag.FNumber, out IExifValue<Rational>? aperture))
                    info.Aperture = $"f/{aperture.Value.ToSingle():F1}";

                if (exif.TryGetValue(ExifTag.ExposureTime, out IExifValue<Rational>? shutter))
                    info.ShutterSpeed = FormatShutterSpeed(shutter.Value.ToSingle());

                if (exif.TryGetValue(ExifTag.FocalLength, out IExifValue<Rational>? focal))
                    info.FocalLength = (int)focal.Value.ToSingle();

                if (exif.TryGetValue(ExifTag.Orientation, out IExifValue<ushort>? orientation))
                    info.Orientation = orientation.Value;

                if (exif.TryGetValue(ExifTag.GPSLatitude, out IExifValue<Rational[]>? lat) &&
                    exif.TryGetValue(ExifTag.GPSLatitudeRef, out IExifValue<string>? latRef))
                {
                    info.Latitude = ConvertGpsCoordinate(lat.Value.Select(r => r.ToSingle()).ToArray(), latRef.Value);
                }
                if (exif.TryGetValue(ExifTag.GPSLongitude, out IExifValue<Rational[]>? lon) &&
                    exif.TryGetValue(ExifTag.GPSLongitudeRef, out IExifValue<string>? lonRef))
                {
                    info.Longitude = ConvertGpsCoordinate(lon.Value.Select(r => r.ToSingle()).ToArray(), lonRef.Value);
                }
            }
        }
        catch
        {
        }
    }

    private static MediaType GetMediaType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".tiff" or ".tif" or ".webp" or ".heic" or ".heif" => MediaType.Image,
            ".mp4" or ".mov" or ".avi" or ".mkv" or ".webm" or ".wmv" or ".flv" or ".m4v" => MediaType.Video,
            _ => MediaType.Unknown
        };
    }

    private static string FormatShutterSpeed(float shutter)
    {
        if (shutter >= 1) return $"{shutter:F0}s";
        return $"1/{(int)(1 / shutter)}s";
    }

    private static double ConvertGpsCoordinate(float[] values, string reference)
    {
        double degrees = values[0];
        double minutes = values[1];
        double seconds = values.Length > 2 ? values[2] : 0;
        double coordinate = degrees + (minutes / 60) + (seconds / 3600);
        if (reference is "S" or "W")
            coordinate = -coordinate;
        return coordinate;
    }
}
