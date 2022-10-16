using System.Reflection;
using System.Runtime.CompilerServices;
using SixLabors.ImageSharp.PixelFormats;

// Sealing the class to get tinny performance boost.  With the class sealed more optimizations can be made because references to this call can only be this class (no overrides allowed)
// |        Method |                       Arguments |     N |        Mean |     Error |    StdDev |      Median |
// |-------------- |-------------------------------- |------ |------------:|----------:|----------:|------------:|
// | FindAllColors | /p:TESTFLAG=true,/t:Clean;Build |     0 |   326.25 ms |  6.389 ms | 10.497 ms |   321.77 ms |
// | FindAllColors |                  /t:Clean;Build |     0 |   334.43 ms |  6.617 ms |  8.126 ms |   332.17 ms |
// TestFlag was with it sealed, As you can see change is small even on 100000 calls. Just it just barely above Error and even within on of the StdDev
public sealed class CharMap
{
    public static CharMap FromFile(string filePath) => new (CharMapData.FromString(File.ReadAllText(filePath)));

    public CharMap(int[] counts, int width, int height, bool[,] backgroundsToSkip, bool[,] foregroundsToSkip, Rgb24[] namedColors)
    {
        MaxX = width;
        MaxY = height;
        m_maxArea = MaxX * MaxY;

        var selectedChars = new Dictionary<int, int>(); // count to charIndex
        for (int charIndex = 0; charIndex < counts.Length; charIndex++)
        {
            var count = counts[charIndex];
            if (count > -1 && !selectedChars.ContainsKey(count))
            {
                selectedChars.Add(count, charIndex);
            }
        }

        m_counts = new (int Count, int Char)[selectedChars.Count];

        int index = default;
        foreach (int count in selectedChars.Keys.OrderBy(x => x))
        {
            m_counts[index++] = (Count: count, Char: selectedChars[count]);
        }

        m_uniqueChars = selectedChars.Values
            .Where(x => x > MinChar)
            .OrderBy(x => x)
            .Select(x => (char)x).ToArray();

        BackgroundsToSkip = backgroundsToSkip;
        ForegroundsToSkip = foregroundsToSkip;

        m_namedColors = namedColors;
    }

    public CharMap(CharMapData data)
        : this(
            data.GetDataCounts(),
            data.MaxX,
            data.MaxY,
            ColorUtilities.BruteForce.DeserializerStaticSkips(data.BackgroundsToSkip),
            ColorUtilities.BruteForce.DeserializerStaticSkips(data.ForegroundsToSkip),
            data.GetDataNamedColors()
        ) { }

    public CharMapData ToCharMapData()
    {
        var counts = new Dictionary<char, int>();
        foreach (var item in m_counts)
        {
            counts[(char)item.Char] = item.Count;
        }
        return new CharMapData{
            MaxX = MaxX,
            MaxY = MaxY,
            Counts = counts,
            BackgroundsToSkip = ColorUtilities.BruteForce.SerializerStaticSkips(BackgroundsToSkip),
            ForegroundsToSkip = ColorUtilities.BruteForce.SerializerStaticSkips(ForegroundsToSkip),
            NamedColors = CharMapData.ConvertKnownColors(m_namedColors),
        };
    }

    public readonly int MaxX;
    public readonly int MaxY;

    // These represents which background/foreground colors can be ignored.  The first index the value determined by ColorIndex.
    // The second value is the just console color casted to int.
    // This works b/c for every region of our color space there are some colors that will never make a good selection
    // color.
    public readonly bool[,] BackgroundsToSkip;

    public readonly bool[,] ForegroundsToSkip;

    /// <summary>
    /// This method is a little slower than PickFromCountWithCount, but it is more accurate
    /// Using this over the following brought the average error from 0.08464973265612824 to less than 0.084 and the max error from almost 2.5 to less than 2 
    /// int count = (int)Math.Round(t * maxPixels);
    /// (char c, int numberOfPixels) = map.PickFromCountWithCount(count);
    /// double pixelRatio = numberOfPixels / maxPixels;
    /// And by a little slower it was 3 or so ms for 100000, and was well withing both the Error and StdDev of the two
    /// </summary>
    public (char Character, double PixelRatio) PickFromRatio(double ratio)
    {
        int target = (int)(ratio * m_maxArea);
        int index = PickFromCountIndex(target);
        (int count, int character) = m_counts[index];
        double match = (double)count / m_maxArea;
        if (count <= target)
        {
            // We truncated our target above, so we could be off by at most one index
            // This can only happen when count is not greater than the target, if the count is over the target, then the truncation did not matter
            int nextIndex = index + 1;
            if (nextIndex < m_counts.Length)
            {
                (int nextCount, int nextCharacter) = m_counts[nextIndex];
                double nextMatch = (double)nextCount / m_maxArea;

                //
                // ration - match will never be negative, it could be zero or greater (we are in this branch b/c we undershot the target)
                //
                // nextMatch - ratio, will only ever be positive if nextMatch > target. And we can assert that.
                // if nextMatch was less than the ratio, then we would have chosen that index first. 
                // So it looks like might need Math.Abs here, but nope we don't
                if ((ratio - match) > (nextMatch - ratio))
                {
                    // Yup we found one that was closer, use that one
                    character = nextCharacter;
                    match = nextMatch;
                }
            }
        }
        return ((char)character, match);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public char GetUniqueChar(int id) => m_uniqueChars[id % m_uniqueChars.Length];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rgb24 NamedColor(ConsoleColor color) => m_namedColors[(int)color];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rgb24 NamedColor(int colorIndex) => m_namedColors[colorIndex];

    public IEnumerable<(int Count, int Char)> Counts => m_counts;

    public int UniqueCharLength => m_uniqueChars.Length;

    private int PickFromCountIndex(int target)
    {
        int min = default;
        int max = m_counts.Length;

        int bestDiff = int.MaxValue;
        int bestIndex = -1;

        while ((min <= max) && (min >= default(int)) && (max <= m_counts.Length))
        {
            int mid = (max + min) / 2;

            if (mid == m_counts.Length)
            {
                return bestIndex;
            }

            int var = m_counts[mid].Count;
            if (var == target)
            {
                // If we find the value just return the charter that goes with it
                return mid;
            }

            int diff;
            if (var > target)
            {
                // The value was more than what we want, so move to look in the lower part
                max = mid - 1;
                diff = var - target;
            }
            else
            {
                // The value was less than what we want, so move up
                min = mid + 1;
                diff = target - var;
            }

            if (diff < bestDiff)
            {
                bestIndex = mid;
                bestDiff = diff;
            }
        }
        return bestIndex;
    }

    public override string ToString() => ToCharMapData().ToString();

    private readonly (int Count, int Char)[] m_counts; // maps counts of pixes to a char (right now that char is last one that found that has that count);

    private readonly char[] m_uniqueChars;

    private readonly Rgb24[] m_namedColors;

    public const int MinChar = (int)' '; // Space (skip all the non-printable ones)

    // This here might be reason to keep these 'chars' as ints, doing so would allow up to include char 255 and not overflow in for loops, but the places that are using this appear to be doing so with <
    public const int MaxChar = unchecked((byte)(~default(byte)));

    // We use this in finding matches based on a double ratio, so store this as double to avoid repetitive casting it 
    private readonly double m_maxArea;

    public static readonly string DefaultMapFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "CharMap.yaml");
}