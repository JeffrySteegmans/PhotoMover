using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Serilog;
using Spectre.Console;

namespace PhotoMover.CLI.Processors;

public class RenameProcessor (
    ILogger logger,
    List<string> photoExtensions,
    ProgressColumn[] progressBar) : IProcessor
{
    public void Execute()
    {
        var sourceFolder = AnsiConsole.Ask<string>("What's the location of your photo's?");

        logger.Information(
            "Processing photo's from folder '{folder}'",
            sourceFolder);

        try
        {
            RenamePhotos(
                new DirectoryInfo(sourceFolder));
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error while renaming photo's from folder '{folder}'", sourceFolder);
        }
    }

    private void RenamePhotos(
        DirectoryInfo directory)
    {
        var photos = photoExtensions
            .SelectMany(extension => directory.EnumerateFiles(extension, SearchOption.TopDirectoryOnly))
            .ToList();

        List<FileInfo> orderedPhotos = [];

        AnsiConsole.Status()
            .Start($"Sorting photos in folder {directory.FullName}", _ =>
            {
                orderedPhotos = photos
                    .OrderBy(GetCreationDateTime)
                    .ToList();
            });

        AnsiConsole.Progress()
            .AutoClear(true)
            .HideCompleted(true)
            .Columns(progressBar)
            .Start(progressContext =>
            {
                var renameTask = progressContext.AddTask(directory.Name, new ProgressTaskSettings()
                {
                    MaxValue = photos.Count
                });

                var counter = 1;
                foreach (var photo in orderedPhotos)
                {
                    try
                    {
                        var oldPhotoPath = photo.FullName;
                        var newPhotoPath = Path.Combine(directory.FullName, $"{counter:00000.##}{photo.Extension.ToLower()}");

                        if (File.Exists(newPhotoPath))
                        {
                            File.Move(newPhotoPath, Path.Combine(directory.FullName, $"tmp_{Guid.NewGuid():N}{photo.Extension}"));
                        }

                        photo.MoveTo(newPhotoPath);

                        logger.Verbose(
                            "Renamed '{source}' to '{destination}'",
                            oldPhotoPath,
                            newPhotoPath);

                        counter++;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error while renaming photo '{photo}'", photo.FullName);
                    }
                    finally
                    {
                        renameTask.Increment(1);
                    }
                }

                renameTask.StopTask();
            });

        foreach (var subDirectory in directory.GetDirectories())
        {
            RenamePhotos(
                subDirectory);
        }
    }

    private DateTime GetCreationDateTime(
        FileInfo file)
    {
        try
        {
            using var stream = file.OpenRead();

            var directories = ImageMetadataReader.ReadMetadata(stream);

            // Find the so-called Exif "SubIFD" (which may be null)
            var subIfdDirectory = directories
                .OfType<ExifSubIfdDirectory>()
                .FirstOrDefault();

            DateTime creationDate = default;

            if (subIfdDirectory is not null)
            {
                logger.Verbose("{fileName} - subIfdDirectory not null", file.Name);
                if (subIfdDirectory.Tags.Any(x => x.Type == ExifDirectoryBase.TagDateTimeOriginal))
                {
                    creationDate = subIfdDirectory.GetDateTime(ExifDirectoryBase.TagDateTimeOriginal);
                    logger.Verbose("{fileName} - ExifDirectoryBase.TagDateTimeOriginal: {date}", file.Name, creationDate);
                }

                if (subIfdDirectory.Tags.Any(x => x.Type == ExifDirectoryBase.TagDateTime))
                {
                    creationDate = subIfdDirectory.GetDateTime(ExifDirectoryBase.TagDateTime);
                    logger.Verbose("{fileName} - ExifDirectoryBase.TagDateTime: {date}", file.Name, creationDate);
                }
            }

            if (creationDate != default)
            {
                return creationDate;
            }

            logger.Verbose("{fileName} - creation date not found in exifData => return lastWriteTime {}", file.Name, file.LastWriteTime);

            return file.LastWriteTime;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to get taken date => returning default date");
            return default;
        }
    }
}