using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;

// Sealing the class to get tinny performance boost.  With the class sealed more optimizations can be made because references to this call can only be this class (no overrides allowed)
// |        Method |                       Arguments |     N |        Mean |     Error |    StdDev |      Median |
// |-------------- |-------------------------------- |------ |------------:|----------:|----------:|------------:|
// | FindAllColors | /p:TESTFLAG=true,/t:Clean;Build |     0 |   326.25 ms |  6.389 ms | 10.497 ms |   321.77 ms |
// | FindAllColors |                  /t:Clean;Build |     0 |   334.43 ms |  6.617 ms |  8.126 ms |   332.17 ms |
// TestFlag was with it sealed, As you can see change is small even on 100000 calls. Just it just barely above Error and even within on of the StdDev
public sealed class CharMap
{
    public CharMap(string? fontName = null)
    { 
        if (string.IsNullOrEmpty(fontName))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                fontName = "Andale Mono"; // This works on my mac
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                fontName = "DejaVu Sans Mono"; // This works on my raspberry pi
            }
            else
            {
                // Should come up with a good default for these
                throw new ApplicationException($"No default font for {RuntimeInformation.OSDescription}, the fonts are {string.Join(", ", SystemFonts.Families)}");
            }
        }

        var font = SystemFonts.CreateFont(fontName, 14.0f);

        // The ability to set fonts will like invalidate these values 
        int size = (int)(14 + 1);  // This was found via experimentation

        var visited = new HashSet<string>();
        var counts = new Dictionary<int, int>();
        float penWidth = 1.0f;

        bool needToAddToY = true;

        for (int i = MinChar; i < MaxChar; i++)
        {
            if (i == (int)'_' || i == (int)'│')
            {
                needToAddToY = false;
            }

            if (i == 160)
            {
                // 160 is non-breaking space, and likely to match 32(space) so we don't get much by checked
                // And it seems to cause problems, see https://github.com/SixLabors/ImageSharp.Drawing/issues/92
                continue;
            }

            (var charMap, int count) = ComputeMapForChar(visited, font, i, size, penWidth);

            if (charMap != default)
            {
                counts[count] = i;

                m_charMaps[i] = charMap;
                int localX = charMap.GetLength(default);
                int localY = charMap.GetLength(1);
                MaxX = Math.Max(MaxX, localX);
                MaxY = Math.Max(MaxY, localY);
            }
        }

        m_counts = new (int Count, int Char)[counts.Count];

        int index = default;
        foreach (int count in counts.Keys.OrderBy(x => x))
        {
            m_counts[index++] = (Count: count, Char: counts[count]);
        }

        // there is going to be a leading "space" on most chars, and we will detect that, we will not detect the trailing space, so deal with that here, by adding 1
        // Is that really true? What about '─' (it is supposed to be the full width)
        //                                 '-'
        MaxX++;

        // You would think the same would apply to height, but I find that is not needed if you include '_'
        if (needToAddToY)
        {
            MaxY++;
        }

        m_maxArea = MaxX * MaxY;

        var chars = new List<char>();
        for (int i = MinChar + 1; i < MaxChar; i++)
        {
            if (m_charMaps[i] != null)
            {
                chars.Add((char)i);
            }
        }
        m_uniqueChars = chars.ToArray();
    }

    public int MaxX {get; private set; }
    public int MaxY {get; private set; }

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

    private static (bool[,]?, int) ComputeMapForChar(HashSet<string> visited, Font font, int charIndex, int size, float penWidth)
    {
        using var image = new Image<Rgb24>(size, size);
        Utilities.DrawChar(image, (char)charIndex, x: 0, y: 0, font, new Rectangle(1, 1, size, size), new SolidBrush(Color.White), Pens.Solid(Color.White, penWidth));

        var pixelData = image.GetPixelData();
        int localMaxX = default;
        int localMaxY = default;
        for (int y = default; y < image.Height; y++)
        for (int x = default; x < image.Width; x++)
        {
            if (!pixelData[y, x].IsBlack())
            {
                localMaxY = y;
                if (x > localMaxX)
                {
                    localMaxX = x;
                }
            }
        }

        var hashValue = string.Empty;
        uint current = default;
        int length = default;
        int count = default;
        var charMap = new bool[localMaxX + 1, localMaxY + 1];

        for (int y = default; y < charMap.GetLength(1); y++)
        for (int x = default; x < charMap.GetLength(default); x++)
        {
            if ((length++) == UIntPtr.Size)
            {
                length = default;
                hashValue += $"{current}|";
                current = default;
            }
            else
            {
                current <<= 1;
            }

            if (!pixelData[y, x].IsBlack())
            {
                count++;
                current |= 1;
                charMap[x, y] = true;
            }
        }

        if (length > default(int))
        {
            hashValue += current.ToString();
        }

        if (visited.Contains(hashValue))
        {
            return (null, count);
        }
        else
        {
            visited.Add(hashValue);
            return (charMap, count);
        }
    }

    private readonly bool[][,] m_charMaps = new bool[MaxChar][,]; // First index is which char, the that is followed by (column, row)

    private readonly (int Count, int Char)[] m_counts; // maps counts of pixes to a char (right now that char is last one that found that has that count);

    private readonly char[] m_uniqueChars;

    public const int MinChar = 32; // Space (skip all the non-printable ones)

    // This here might be reason to keep these 'chars' as ints, doing so would allow up to include char 255 and not overflow in for loops, but the places that are using this appear to be doing so with <
    public const int MaxChar = unchecked((byte)(~default(byte)));

    // We use this in finding matches based on a double ratio, so store this as double to avoid repetitive casting it 
    private readonly double m_maxArea;
}