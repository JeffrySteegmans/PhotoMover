using Serilog;
using Spectre.Console;
using PhotoMover.CLI;

Console.Title = "PhotoMover";

await using var logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.File(@$"logs\log-{DateTime.Now:yyMMdd-HHmm}.txt", rollOnFileSizeLimit: true)
    .CreateLogger();

var processor = new PhotoProcessor(
    logger);

var photoFolder = AnsiConsole.Prompt(
    new TextPrompt<string>("What's the location of your photo's?")
        .DefaultValue(@"F:\Fotos"));
var resultFolder = AnsiConsole.Prompt(
    new TextPrompt<string>("Where do you want to store the result?")
        .DefaultValue(@"D:\Fotos"));

var inputPanel = new Panel(new TextPath(photoFolder))
{
    Header = new PanelHeader("Source folder"),
    Padding = new Padding(7, 1, 7, 1)
};
AnsiConsole.Write(inputPanel);

var outputPanel = new Panel(new TextPath(resultFolder))
{
    Header = new PanelHeader("Destination folder"),
    Padding = new Padding(7, 1, 7, 1)
};
AnsiConsole.Write(outputPanel);

var processPhotos = AnsiConsole.Prompt(
    new TextPrompt<bool>($"Continue processing?")
        .AddChoice(true)
        .AddChoice(false)
        .DefaultValue(true)
        .WithConverter(choice => choice ? "y" : "n"));

AnsiConsole.Clear();

if (processPhotos)
{
    await processor.Process(
        photoFolder,
        resultFolder);
}

AnsiConsole.Write(
    new FigletText("DONE")
        .LeftJustified()
        .Color(Color.Green));

// Console.WriteLine("Getting photo's...");
// var photos = new Dictionary<int, ICollection<Photo>>();
// PhotoProcessor.GetPhotos(startFolder, photos);
//
// Console.WriteLine("Sorting photo's...");
// PhotoProcessor.SortPhotos(photos);
//
// Console.WriteLine("Copying photo's...");
// PhotoProcessor.CopyPhotos(photos, resultsFolder);
//
// Console.WriteLine($"{Environment.NewLine}DONE");