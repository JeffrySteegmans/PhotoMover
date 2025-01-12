using Serilog;
using Spectre.Console;
using PhotoMover.CLI;
using PhotoMover.CLI.Processors;

Console.Title = "PhotoMover";

List<string> PhotoExtensions = ["*.jpg", "*.jpeg", "*.png"];
List<string> VideoExtensions = ["*.mov", "*.mp4", "*.avi", "*.wmv"];
ProgressColumn[] ProgressBar =
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

await using var logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.File(@$"logs\log-{DateTime.Now:yyMMdd-HHmm}.txt", rollOnFileSizeLimit: true)
    .CreateLogger();

var processors = new Dictionary<string, IProcessor>
{
    {
        "Photo mover", new PhotoMoverProcessor(
            logger,
            PhotoExtensions,
            ProgressBar)
    },
    {
        "Rename photo's", new RenameProcessor(
            logger,
            PhotoExtensions,
            ProgressBar)
    },
    {
        "Rename videos", new RenameProcessor(
            logger,
            VideoExtensions,
            ProgressBar)
    },
    {
        "Clean folders", new CleanFolderProcessor(
            logger,
            PhotoExtensions.Concat(VideoExtensions).ToList())
    }
};

var processorKey = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("Choose your action?")
        .PageSize(10)
        .MoreChoicesText("[grey](Move up and down to reveal more actions)[/]")
        .AddChoices(processors.Keys));

processors[processorKey]
    .Execute();

AnsiConsole.Write(
    new FigletText("DONE")
        .LeftJustified()
        .Color(Color.Green));