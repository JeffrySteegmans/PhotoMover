using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using PhotoMover.CLI.Models;
using Directory = System.IO.Directory;

namespace PhotoMover.CLI;

internal static class PhotoProcessor
{
    private static readonly List<string> AllowedExtensions = new () { ".jpg", ".jpeg", ".png" };
    
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
        return !AllowedExtensions.Contains(Path.GetExtension(filePath).ToLower());
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
    
    public static Dictionary<int, ICollection<Photo>> GroupAndSortPhotos(List<Photo> photos)
    {
        var groupedAndSortedPhotos = new Dictionary<int, ICollection<Photo>>();
        
        foreach (var photo in photos)
        {
            var year = photo.TakenDateTime.Year;

            if (groupedAndSortedPhotos.TryGetValue(year, out ICollection<Photo>? value))
            {
                value.Add(photo);
            }
            else
            {
                groupedAndSortedPhotos.Add(year, new List<Photo> { photo });    
            }

            groupedAndSortedPhotos[year] = groupedAndSortedPhotos[year].OrderBy(x => x.TakenDateTime).ToList();
        }

        return groupedAndSortedPhotos;
    }

    public static void CopyPhotos(Dictionary<int, ICollection<Photo>> groupedAndSortedPhotos, string resultsFolder)
    {
        foreach (var (year, photos) in groupedAndSortedPhotos)
        {
            var counter = 1;

            if (!Directory.Exists($@"{resultsFolder}\{year}\"))
            {
                Directory.CreateDirectory($@"{resultsFolder}\{year}\");
            }
            
            foreach (var photo in photos)
            {
                Console.WriteLine($"Found {photo.FileName}{photo.FileExtension} with date: {photo.TakenDateTime}");
                File.Copy(photo.FilePath,$@"{resultsFolder}\{year}\{counter:D5}.jpg");
                counter++;
            }
        }
    }
}