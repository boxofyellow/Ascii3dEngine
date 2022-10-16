using SixLabors.ImageSharp.PixelFormats;
using YamlDotNet.Serialization;

public class CharMapData
{
    public int MaxX;
    public int MaxY;

    public Dictionary<char, int> Counts = new();

    public Dictionary<ConsoleColor, Rgb24> NamedColors = new();

    public string[] BackgroundsToSkip = Array.Empty<string>();
    public string[] ForegroundsToSkip = Array.Empty<string>();

    public override string ToString()
    {
        ISerializer serializer = new SerializerBuilder().Build();
        return serializer.Serialize(this);
    }

    public static CharMapData FromString(string data)
        => new DeserializerBuilder()
            .Build()
            .Deserialize<CharMapData>(data);

    public int[] GetDataCounts()
    {
        var result = new int[CharMap.MaxChar + 1];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = -1;
        }
        foreach (var item in Counts)
        {
            result[item.Key] = item.Value;
        }
        return result;
    }

    public Rgb24[] GetDataNamedColors()
    {
        var result = new Rgb24[NamedColors.Count];
        foreach (var color in ColorUtilities.ConsoleColors)
        {
            result[(int)color] = NamedColors[color];
        }
        return result;
    }

    public static Dictionary<ConsoleColor, Rgb24> ConvertKnownColors(Rgb24[] colors)
    {
        var result = new Dictionary<ConsoleColor, Rgb24>();
        foreach (var color in ColorUtilities.ConsoleColors)
        {
            result[color] = colors[(int)color];
        }
        return result;
    }
}