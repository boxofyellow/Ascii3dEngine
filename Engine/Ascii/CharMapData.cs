using YamlDotNet.Serialization;

public class CharMapData
{
    public int MaxX;
    public int MaxY;

    public Dictionary<char, int> Counts = new();

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
        var counts = new int[CharMap.MaxChar + 1];
        for (int i = 0; i < counts.Length; i++)
        {
            counts[i] = -1;
        }
        foreach (var item in Counts)
        {
            counts[item.Key] = item.Value;
        }
        return counts;
    }
}