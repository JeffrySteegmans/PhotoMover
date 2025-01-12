using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Serilog;
using Spectre.Console;
using FileInfo = System.IO.FileInfo;

namespace PhotoMover.CLI.Processors;

internal class PhotoMoverProcessor(
    ILogger logger,
    List<string> allowedExtensions,
    ProgressColumn[] progressBar) : IProcessor
{
    public void Execute()
    {
        var sourceFolder = AnsiConsole.Ask<string>("What's the location of your photo's?");
        var destinationFolder = AnsiConsole.Ask<string>("Where do you want to store the result?");

        AnsiConsole.Write(new Panel(new TextPath(sourceFolder))
        {
            Header = new PanelHeader("Source folder"),
            Padding = new Padding(7, 1, 7, 1)
        });
        AnsiConsole.Write(new Panel(new TextPath(destinationFolder))
        {
            Header = new PanelHeader("Destination folder"),
            Padding = new Padding(7, 1, 7, 1)
        });

        var startProcessing = AnsiConsole.Prompt(
            new TextPrompt<bool>($"Continue processing?")
                .AddChoice(true)
                .AddChoice(false)
                .DefaultValue(true)
                .WithConverter(choice => choice ? "y" : "n"));

        if (!startProcessing)
        {
            AnsiConsole.Write(
                new FigletText("CANCELLED")
                    .LeftJustified()
                    .Color(Color.Red));

            return;
        }

        AnsiConsole.Clear();

        logger.Information(
            "Processing photo's from folder '{inputFolder}' into '{outputFolder}'",
            sourceFolder,
            destinationFolder);

        AnsiConsole.Progress()
            .AutoClear(true)
            .HideCompleted(true)
            .Columns(progressBar)
            .Start(progressContext =>
            {
                try
                {
                    MovePhotos(
                        new DirectoryInfo(sourceFolder),
                        new DirectoryInfo(destinationFolder),
                        progressContext);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error while moving photo's from folder '{folder}'", sourceFolder);
                }
            });
    }

    private void MovePhotos(
        DirectoryInfo inputFolder,
        DirectoryInfo outputFolder,
        ProgressContext progressContext)
    {
        var photos = allowedExtensions
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

            var newPhotoNumber = allowedExtensions
                .SelectMany(x => destinationFolder.EnumerateFiles(x, SearchOption.TopDirectoryOnly))
                .Count() + 1;

            var destinationPhoto = Path.Combine(
                destinationFolder.FullName,
                $"{newPhotoNumber:00000.##}{sourcePhoto.Extension}");

            var sourceFileName = sourcePhoto.FullName;

            sourcePhoto.MoveTo(
                destinationPhoto);

            logger.Verbose(
                "Moved '{source}' to '{destination}'",
                sourceFileName,
                destinationPhoto);

            moveTask.Increment(1);
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

            logger.Verbose("{fileName} - creation date not found in exifdata => return lastWriteTime {}", file.Name, file.LastWriteTime);

            return file.LastWriteTime;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to get taken date => returning default date");
            return default;
        }
    }
}