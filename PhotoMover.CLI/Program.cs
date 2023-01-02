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
var photos = new List<Photo>();

PhotoProcessor.GetPhotos(startFolder, photos);
var groupedAndSortedPhotos = PhotoProcessor.GroupAndSortPhotos(photos);
PhotoProcessor.CopyPhotos(groupedAndSortedPhotos, resultsFolder);

Console.WriteLine($"{Environment.NewLine}DONE");