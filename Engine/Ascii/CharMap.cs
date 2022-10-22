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

        m_consoleColors = namedColors;

        // So we can look at matching a color as looking through all the colors that we can make to find the one that matches the best.
        // We can make colors by selecting two console colors, and we can "mix" them by selecting a character
        // the more pixels the character uses, the more of the foreground color will be shown
        // So we are effectively look a version of the Nearest neighbor problem (https://en.wikipedia.org/wiki/Nearest_neighbor_search)
        // Our color components R, G, B (0-255) will be our X, Y, Z coordinates.
        //
        // This can be a little problematic, experimentation shows we can make some 11K unique colors.
        //
        // One thing to note is that the colors that we can create are NOT evenly distributed in our Color space
        // They are all spread out alone lines between the two Foreground/Background colors.
        //
        // So maybe we can do better...
        // We should be able to search by finding the line that is closest to our target color
        // https://www.youtube.com/watch?v=g2h3H0FkLjA
        // We have two colors (Foreground and Background) and we can express them as
        // r(t) = Background + t*(Foreground - Background)
        // Here r(t) will be the point on that line
        // r(t) = a + t*v
        // a will be our starting point (our Background) and v will be vector from Background to Foreground
        // a =
        //    | Background.R |
        //    | Background.G |
        //    | Background.B |
        // v =
        //    | Foreground.R - Background.R |
        //    | Foreground.G - Background.G |
        //    | Foreground.B - Background.B |
        // r(t) =
        //    | Background.R + t * (Foreground.R - Background.R) |
        //    | Background.G + t * (Foreground.G - Background.G) |
        //    | Background.B + t * (Foreground.B - Background.B) |
        // and the point will be "target" that we want to get close too soo
        // p = target
        //    | target.R |
        //    | target.G |
        //    | target.B |
        // b will be vector from our target point to the point c (the closest point on the line, which will be r(t) when t lines up correctly)
        // b = c - p  (we are going to want the length of this vector)
        // b = r(t) - p
        //    | Background.R + t * (Foreground.R - Background.R) - target.R |
        //    | Background.G + t * (Foreground.G - Background.G) - target.G |
        //    | Background.B + t * (Foreground.B - Background.B) - target.B |
        //
        // v X b = 0 (b/c a and b will be perpendicular )
        //    | Foreground.R - Background.R |   | Background.R + t * (Foreground.R - Background.R) - target.R |
        //    | Foreground.G - Background.G | X | Background.G + t * (Foreground.G - Background.G) - target.G |
        //    | Foreground.B - Background.B |   | Background.B + t * (Foreground.B - Background.B) - target.B |
        //
        // 0 = (Foreground.R - Background.R) * (Background.R + t * (Foreground.R - Background.R) - target.R)
        //   + (Foreground.G - Background.G) * (Background.G + t * (Foreground.G - Background.G) - target.G)
        //   + (Foreground.B - Background.B) * (Background.B + t * (Foreground.B - Background.B) - target.B)
        //
        // vR, the Red component of V = Foreground.R - Background.R
        // vG, the Green component of V = Foreground.G - Background.G
        // vB, the Blue component of V = Foreground.B - Background.B
        //
        // 0 = (vR) * (Background.R + (t * vR) - target.R)
        //   + (vG) * (Background.G + (t * vG) - target.G)
        //   + (vB) * (Background.B + (t * vB) - target.B)
        //
        // vR * (target.R - Background.R) + vG * (target.G - Background.G) + vB * (target.B - Background.B)
        //   =
        // t * (vR^2 + vG^2 + vB^2)
        //
        //      vR * (target.R - Background.R) + vG * (target.G - Background.G) + vB * (target.B - Background.B)
        // t = -------------------------------------------------------------------------------------------------
        //     (vR^2 + vG^2 + vB^2)
        //
        // We are going to compute these bunch so lets cache them
        // 
        //  t = ((stuff WITH target) + (stuff withOUT target))/(OTHER stuff withOUT target)
        //  (stuff WITH target)          = vR * target.R + vG * target.G + vB * target.B
        //  (stuff withOUT target)       = - (vR * Background.R + vG * Background.G + vB * Background.B)
        //  (OTHER stuff withOUT target) = (vR^2 + vG^2 + vB^2)

        m_cachedDenominators = new double[m_consoleColors.Length, m_consoleColors.Length];
        m_cachedStaticNumeratorDenominators = new double[m_consoleColors.Length, m_consoleColors.Length];
        for (int i = 0; i < m_consoleColors.Length; i++)
        {
            // this will be our background color
            var p1 = m_consoleColors[i];
            int p1R = p1.R;
            int p1G = p1.G;
            int p1B = p1.B;
            for (int j = 0; j < m_consoleColors.Length; j++)
            {
                if (i != j)
                {
                    // this will be our foreground color
                    var p2 = m_consoleColors[j];

                    int vR = p2.R - p1R;
                    int vG = p2.G - p1G;
                    int vB = p2.B - p1B;

                    m_cachedStaticNumeratorDenominators[i, j] = -((vR * p1R) + (vG * p1G) + (vB * p1B));
                    m_cachedDenominators[i, j] = (vR * vR) + (vG * vG) + (vB * vB);
                }
            }
        }
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
            NamedColors = CharMapData.ConvertKnownColors(m_consoleColors),
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
    public Rgb24 NamedColor(ConsoleColor color) => m_consoleColors[(int)color];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rgb24 NamedColor(int colorIndex) => m_consoleColors[colorIndex];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double CachedDenominators(int selectedIndex, int secondIndex) => m_cachedDenominators[selectedIndex, secondIndex];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double CachedStaticNumeratorDenominators(int selectedIndex, int secondIndex) => m_cachedStaticNumeratorDenominators[selectedIndex, secondIndex];

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

    private readonly Rgb24[] m_consoleColors;

    private readonly double[,] m_cachedDenominators;
    private readonly double[,] m_cachedStaticNumeratorDenominators;

    public const int MinChar = (int)' '; // Space (skip all the non-printable ones)

    // This here might be reason to keep these 'chars' as ints, doing so would allow up to include char 255 and not overflow in for loops, but the places that are using this appear to be doing so with <
    public const int MaxChar = unchecked((byte)(~default(byte)));

    // We use this in finding matches based on a double ratio, so store this as double to avoid repetitive casting it 
    private readonly double m_maxArea;

    public static readonly string DefaultMapFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "CharMap.yaml");
}