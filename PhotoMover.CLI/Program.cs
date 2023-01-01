using PhotoMover.CLI;
using PhotoMover.CLI.Models;
using Directory = System.IO.Directory;

Console.Title = "PhotoMover";

//Console.Write("Start folder: ");
var startFolder = @"C:\PhotoMover test files\Start files";// Console.ReadLine();

// if (startFolder is null)
// {
//     Environment.Exit(1);
// }

//Console.Write("Results folder: ");
var resultsFolder = @"C:\PhotoMover test files\Results"; //Console.ReadLine();

// if (resultsFolder is null)
// {
//     Environment.Exit(1);
// }

Console.WriteLine("Getting photo's...");

var photos = new List<Photo>();
PhotoProcessor.GetPhotos(startFolder, photos);

var counters = new Dictionary<int, int>();
foreach (var photo in photos)
{
    var year = photo.TakenDateTime.Year;

    if (!Directory.Exists($@"{resultsFolder}\{year}\"))
    {
        Directory.CreateDirectory($@"{resultsFolder}\{year}\");
    }

    if (counters.ContainsKey(year))
    {
        counters[year]++;
    }
    else
    {
        counters.Add(year, 1);
    }
    
    Console.WriteLine($"Found {photo.FileName}{photo.FileExtension} with date: {photo.TakenDateTime}");
    File.Copy(photo.FilePath,$@"{resultsFolder}\{year}\{counters[year]:D5}.jpg");
}

Console.WriteLine($"{Environment.NewLine}DONE");