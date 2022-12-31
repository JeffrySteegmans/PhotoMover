Console.Title = "PhotoMover";

var _extensions = new List<string> { ".jpg", ".jpeg", ".png" };

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

var photos = new List<string>();
Console.WriteLine("Indexing photo's...");
foreach (var subfolder in Directory.GetDirectories(startFolder))
{
    Console.Write($"Scanning folder '{subfolder}': ");
    var photoCounter = 0;
    foreach (var file in Directory.GetFiles(subfolder))
    {
        if (_extensions.Any(x => file.EndsWith(x, StringComparison.InvariantCultureIgnoreCase)))
        {
            photos.Add(file);
            photoCounter++;
        }
    }
    Console.WriteLine($"{photoCounter} photo's found in subfolder");
}

Console.WriteLine($"Found {photos.Count} photo's.");

Console.WriteLine($"Copying photo files from {startFolder} to {resultsFolder}");

var counter = 1;
foreach (var photo in photos)
{
    File.Copy(photo,$@"{resultsFolder}\{counter:D4}.jpg");
    counter++;
}