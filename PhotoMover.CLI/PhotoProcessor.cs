using System.Collections.Concurrent;
using System.Diagnostics;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Serilog;
using Serilog.Core;
using Spectre.Console;
using Directory = System.IO.Directory;
using FileInfo = System.IO.FileInfo;

namespace PhotoMover.CLI;

internal class PhotoProcessor(
    ILogger logger)
{
    private static readonly List<string> AllowedExtensions = new () { "*.jpg", "*.jpeg", "*.png" };
    private static ProgressColumn[] _progressBar =
    [
        new TaskDescriptionColumn(),    // Task description
        new ProgressBarColumn
        {
            CompletedStyle = new Style(Color.Green)
        },
        new PercentageColumn(),         // Percentage
        new RemainingTimeColumn(),      // Remaining time
        new SpinnerColumn() // Spinner
    ];

    public async Task Process(
        string inputFolder,
        string outputFolder)
    {
        logger.Information(
            "Processing photo's from folder '{inputFolder}' into '{outputFolder}'",
            inputFolder,
            outputFolder);

        var photos = new List<FileInfo>();

        AnsiConsole.Status()
            .Start($"Searching all photo's in '{inputFolder}'...", ctx =>
            {
                logger.Debug(
                    "Searching for all photo's in '{folder}'",
                    inputFolder);

                var directoryInfo = new DirectoryInfo(inputFolder);
                var files = AllowedExtensions
                    .SelectMany(x => directoryInfo.EnumerateFiles(x, SearchOption.AllDirectories));
                photos.AddRange(files);
            });

        AnsiConsole.MarkupLine($"[green]Found [bold]{photos.Count}[/] photo's[/]");

        var indexedPhotos = new Dictionary<int, List<FileInfo>>();

        AnsiConsole.Progress()
            .AutoClear(true)
            .HideCompleted(true)
            .Columns(_progressBar)
            .Start(ctx =>
            {
                var totalPhotos = photos.Count;
                var currentPhoto = 1;

                var indexingPhotosTask = ctx.AddTask("Indexing photos", new ProgressTaskSettings()
                {
                    MaxValue = photos.Count,
                });

                foreach (var photo in photos)
                {
                    var originalDateTime = GetOriginalDateTime(photo);

                    if (!indexedPhotos.TryGetValue(originalDateTime.Year, out var value))
                    {
                        value = [];
                        indexedPhotos.Add(originalDateTime.Year, value);
                    }

                    value.Add(photo);

                    logger.Verbose(
                        "{counter}/{totalPhotos}: '{photo.Name}' => year = {originalDateTime.Year}",
                        currentPhoto,
                        totalPhotos,
                        photo.Name,
                        originalDateTime.Year);

                    currentPhoto++;
                    indexingPhotosTask.Increment(1);
                }
            });
        AnsiConsole.MarkupLine($"[green]Found [bold]{indexedPhotos.Keys.Count}[/] years[/]");
        AnsiConsole.Write(new Panel(string.Join(Environment.NewLine, indexedPhotos.Keys)) {
            Header = new PanelHeader("Years"),
            Padding = new Padding(2, 1, 2, 1)
        });

        AnsiConsole.Progress()
            .AutoClear(true)
            .HideCompleted(true)
            .Columns(_progressBar)
            .Start(ctx =>
            {
                var movePhotosTask = ctx.AddTask("Move photos", new ProgressTaskSettings()
                {
                    MaxValue = photos.Count
                });

                foreach (var year in indexedPhotos.Keys.Order())
                {
                    var yearPath = Path.Combine(outputFolder, year.ToString());
                    var photosOfYear = indexedPhotos[year];
                    var yearCounter = 1;

                    if (!Directory.Exists(yearPath))
                    {
                        Directory.CreateDirectory(yearPath);
                    }

                    var yearTask = ctx.AddTask($"Year {year}", new ProgressTaskSettings()
                    {
                        MaxValue = photosOfYear.Count
                    });

                    foreach (var photo in photosOfYear)
                    {
                        File.Move(
                            photo.FullName,
                            Path.Combine(yearPath, $"{yearCounter:0000.##}{photo.Extension}"),
                            false);

                        logger.Verbose(
                            "{counter}/{total}: '{photo.Name}' => Moved",
                            yearCounter,
                            photosOfYear.Count,
                            photo.Name);

                        yearTask.Increment(1);
                        movePhotosTask.Increment(1);
                        yearCounter++;
                    }
                }
            });
    }

    private DateTime GetOriginalDateTime(
        FileInfo file)
    {
        try
        {
            var directories = ImageMetadataReader.ReadMetadata(file.OpenRead());

            // Find the so-called Exif "SubIFD" (which may be null)
            var subIfdDirectory = directories
                .OfType<ExifSubIfdDirectory>()
                .FirstOrDefault();

            // Read the DateTime tag value
            return subIfdDirectory?
                .GetDateTime(ExifDirectoryBase.TagDateTimeOriginal) ?? default;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to get taken date => returning default date");
            return default;
        }
    }

    private List<string> FindSubDirectories(
        string folder,
        Action<string> updateStatus)
    {
        var subDirectories = new List<string>();

        foreach (var subDirectory in Directory.EnumerateDirectories(folder))
        {
            if (subDirectories.Contains(subDirectory))
            {
                continue;
            }

            subDirectories.Add(subDirectory);
            updateStatus(subDirectory);
            logger.Verbose(
                "Processing folder '{folder}'",
                subDirectory);
            subDirectories.AddRange(FindSubDirectories(subDirectory, updateStatus));
        }

        return subDirectories;
    }


    // internal static void GetPhotos(string folder, Dictionary<int, ICollection<Photo>> photos)
    // {
    //     Console.WriteLine($"Searching folder: {folder}");
    //     foreach (var filePath in Directory.GetFiles(folder))
    //     {
    //         if (HasInvalidExtension(filePath))
    //         {
    //             continue;
    //         }
    //         Console.WriteLine($"\tFound photo: {filePath}");
    //
    //         var takenDateTime = GetTakenDateTime(filePath);
    //
    //         var photo = Photo.CreatePhoto(filePath, takenDateTime);
    //
    //         var year = photo.TakenDateTime.Year;
    //
    //         if (photos.TryGetValue(year, out ICollection<Photo>? value))
    //         {
    //             value.Add(photo);
    //         }
    //         else
    //         {
    //             photos.Add(year, new List<Photo> { photo });
    //         }
    //     }
    //
    //     foreach (var subFolder in Directory.GetDirectories(folder))
    //     {
    //         GetPhotos(subFolder, photos);
    //     }
    // }
    //
    // private static bool HasInvalidExtension(string filePath)
    // {
    //     return !AllowedExtensions.Contains(Path.GetExtension(filePath).ToLower());
    // }
    //
    // public static void SortPhotos(Dictionary<int, ICollection<Photo>> groupedPhotos)
    // {
    //     foreach (var (year, photos) in groupedPhotos)
    //     {
    //         groupedPhotos[year] = photos.OrderBy(x => x.TakenDateTime).ToList();
    //     }
    // }
    //
    // public static void CopyPhotos(Dictionary<int, ICollection<Photo>> groupedAndSortedPhotos, string resultsFolder)
    // {
    //     foreach (var (year, photos) in groupedAndSortedPhotos)
    //     {
    //         var counter = 1;
    //
    //         if (!Directory.Exists($@"{resultsFolder}\{year}\"))
    //         {
    //             Directory.CreateDirectory($@"{resultsFolder}\{year}\");
    //         }
    //
    //         foreach (var photo in photos)
    //         {
    //             Console.WriteLine($"Found {photo.FileName}{photo.FileExtension} with date: {photo.TakenDateTime}");
    //             File.Copy(photo.FilePath,$@"{resultsFolder}\{year}\{counter:D5}.jpg");
    //             counter++;
    //         }
    //     }
    // }
}