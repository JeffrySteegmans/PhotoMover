using PhotoMover.CLI;
using PhotoMover.CLI.Models;

Console.Title = "PhotoMover";

Console.Write("Start folder: ");
var startFolder = Console.ReadLine();
if (startFolder is null)
{
    Environment.Exit(1);
}

Console.Write("Results folder: ");
var resultsFolder = Console.ReadLine();
if (resultsFolder is null)
{
    Environment.Exit(1);
}

Console.WriteLine("Getting photo's...");
var photos = new Dictionary<int, ICollection<Photo>>();
PhotoProcessor.GetPhotos(startFolder, photos);

Console.WriteLine("Sorting photo's...");
PhotoProcessor.SortPhotos(photos);

Console.WriteLine("Copying photo's...");
PhotoProcessor.CopyPhotos(photos, resultsFolder);

Console.WriteLine($"{Environment.NewLine}DONE");