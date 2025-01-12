using Serilog;
using Spectre.Console;

namespace PhotoMover.CLI.Processors;

public class CleanFolderProcessor(
    ILogger logger,
    List<string> extensionsToCheck) : IProcessor
{
    public void Execute()
    {
        var sourceFolder = AnsiConsole.Ask<string>("What's the location of your photo's?");

        logger.Information(
            "Clean folder '{folder}'",
            sourceFolder);

        AnsiConsole.Status()
            .Start($"Clean folder", _ =>
            {
                CleanFolder(
                    new DirectoryInfo(sourceFolder));
            });
    }

    private void CleanFolder(
        DirectoryInfo sourceFolder)
    {
        var files = extensionsToCheck
            .SelectMany(extension => sourceFolder.EnumerateFiles(extension, SearchOption.AllDirectories))
            .ToList();

        if (!files.Any())
        {
            logger.Information(
                "No media files found in folder => cleanup");

            sourceFolder.Delete(true);
            return;
        }

        logger.Information(
            "Media files found in folder or subfolders => check subfolders");

        foreach (var subDirectory in sourceFolder.GetDirectories())
        {
            CleanFolder(
                subDirectory);
        }
    }
}