using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using PhotoMover.CLI.Models;
using Directory = System.IO.Directory;

namespace PhotoMover.CLI;

internal static class PhotoProcessor
{
    private static List<string> _allowedExtensions = new () { ".jpg", ".jpeg", ".png" };
    
    internal static void GetPhotos(string folder, ICollection<Photo> photos)
    {
        foreach (var filePath in Directory.GetFiles(folder))
        {
            if (HasInvalidExtension(filePath))
            {
                continue;
            }

            var takenDateTime = GetTakenDateTime(filePath);

            var photo = Photo.CreatePhoto(filePath, takenDateTime);
            photos.Add(photo);
        }

        foreach (var subFolder in Directory.GetDirectories(folder))
        {
            GetPhotos(subFolder, photos);
        }
    }

    private static bool HasInvalidExtension(string filePath)
    {
        return !_allowedExtensions.Contains(Path.GetExtension(filePath).ToLower());
    }

    private static DateTime GetTakenDateTime(string filePath)
    {
    
        var directories = ImageMetadataReader.ReadMetadata(filePath);
        
        // Find the so-called Exif "SubIFD" (which may be null)
        var subIfdDirectory = directories
            .OfType<ExifSubIfdDirectory>()
            .FirstOrDefault();

        // Read the DateTime tag value
        return subIfdDirectory?
            .GetDateTime(ExifDirectoryBase.TagDateTimeOriginal) ?? DateTime.MinValue;
    }
}