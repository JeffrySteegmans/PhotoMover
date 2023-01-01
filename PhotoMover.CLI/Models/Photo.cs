namespace PhotoMover.CLI.Models;

internal sealed class Photo
{
    public string FilePath { get; }

    public string FileName { get; }

    public string FileExtension { get; }

    public DateTime TakenDateTime { get; private init; }

    private Photo(string filePath)
    {
        FilePath = filePath;
        FileName = Path.GetFileNameWithoutExtension(filePath);
        FileExtension = Path.GetExtension(filePath);
    }

    internal static Photo CreatePhoto(string filePath, DateTime takenDateTime)
    {
        var photo = new Photo(filePath)
        {
            TakenDateTime = takenDateTime
        };

        return photo;
    }
}