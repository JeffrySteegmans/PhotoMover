using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Serilog;
using Spectre.Console;
using Directory = System.IO.Directory;
using FileInfo = System.IO.FileInfo;

namespace PhotoMover.CLI;

internal class PhotoProcessor(
    ILogger logger)
{
    private static readonly List<string> AllowedExtensions = ["*.jpg", "*.jpeg", "*.png"];
    private static readonly ProgressColumn[] ProgressBar =
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

    public void Process(
        DirectoryInfo inputFolder,
        DirectoryInfo outputFolder)
    {
        logger.Information(
            "Processing photo's from folder '{inputFolder}' into '{outputFolder}'",
            inputFolder,
            outputFolder);

        AnsiConsole.Progress()
            .AutoClear(true)
            .HideCompleted(true)
            .Columns(ProgressBar)
            .Start(progressContext =>
            {
                try
                {
                    MovePhotos(
                        inputFolder,
                        outputFolder,
                        progressContext);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error while moving photo's from folder '{folder}'", inputFolder);
                }
            });
    }

    private void MovePhotos(
        DirectoryInfo inputFolder,
        DirectoryInfo outputFolder,
        ProgressContext progressContext)
    {
        var photos = AllowedExtensions
            .SelectMany(x => inputFolder.EnumerateFiles(x, SearchOption.TopDirectoryOnly))
            .ToList();

        var moveTask = progressContext.AddTask(inputFolder.Name, new ProgressTaskSettings()
        {
            MaxValue = photos.Count
        });

        foreach (var sourcePhoto in photos)
        {
            var originalDateTime = GetOriginalDateTime(sourcePhoto);

            var destinationFolder = new DirectoryInfo(
                Path.Combine(outputFolder.FullName, originalDateTime.Year.ToString()));

            if (!destinationFolder.Exists)
            {
                destinationFolder.Create();
            }

            var newPhotoNumber = AllowedExtensions
                .SelectMany(x => destinationFolder.EnumerateFiles(x, SearchOption.TopDirectoryOnly))
                .Count() + 1;

            var destinationPhoto = Path.Combine(
                destinationFolder.FullName,
                $"{newPhotoNumber:0000.##}{sourcePhoto.Extension}");

            File.Move(
                sourcePhoto.FullName,
                destinationPhoto,
                false);

            moveTask.Increment(1);

            logger.Verbose(
                "Moved '{source}' to '{destination}'",
                sourcePhoto.FullName,
                destinationPhoto);
        }

        moveTask.StopTask();

        foreach (var subDirectory in inputFolder.GetDirectories())
        {
            try
            {
                MovePhotos(
                    subDirectory,
                    outputFolder,
                    progressContext);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while moving photo's from folder '{folder}'", subDirectory);
            }

        }
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
}