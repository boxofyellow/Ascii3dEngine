using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace Ascii3dEngine
{
    public class CharMap
    {
        public CharMap(Settings settings)
        { 
            Font font = SystemFonts.CreateFont("Courier New", 14.0f);
            int size = (int)(14 + 1);  // This was found via extermination

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

                (bool[,] charMap, int count) = ComputeMapForChar(visited, font, i, size, penWidth);

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

            // You would think the same would apply to height, but I that is not needed if you include '_'
            if (needToAddToY)
            {
                MaxY++;
            }

            if (settings.PruneMap)
            {
                Prune();
            }
        }

        public int MaxX {get; private set; }
        public int MaxY {get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasData(int charIndex) => (charIndex == MinChar) || (m_charMaps[charIndex] != default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(int charIndex, int x, int y) => m_charMaps[charIndex][x, y];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LocalX(int charIndex) => m_charMaps[charIndex]?.GetLength(default) ?? default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LocalY(int charIndex) => m_charMaps[charIndex]?.GetLength(1) ?? default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char PickFromCount(int count) => (char)m_counts[PickFromCountIndex(count)].Char;

        /// <summary>
        /// One the the things that really effects the fitting time is how many chars are in the map
        /// We can spend some time upfront to reduce the map to just the "useful" ones
        /// How do we define "useful"... Well, for now lets make sure that for every pixel we can cover we do cover
        /// This implementation drops the map size from 150+ to like 10.  
        /// </summary>
        private void Prune()
        {
            int start = 0;
            var matches = new HashSet<int>[MaxX, MaxY];
            for (int i = MinChar; i < MaxChar; i++)
            {
                if (m_charMaps[i] != null)
                {
                    start++;
                    for (int x = 0; x < LocalX(i); x++)
                    for (int y = 0; y < LocalY(i); y++)
                    {
                        if (m_charMaps[i][x, y])
                        {
                            if (matches[x, y] == null)
                            {
                                matches[x, y] = new HashSet<int>();
                            }
                            matches[x, y].Add(i);
                        }
                    }
                }
            }

            var needed = new HashSet<int>();

            bool keepGoing = true;
            for (int count = 1; keepGoing; count++)
            {                
                keepGoing = false;
                for (int x = 0; x < MaxX; x++)
                for (int y = 0; y < MaxY; y++)
                {
                    if (matches[x, y] != null)
                    {
                        keepGoing = true;
                        if (matches[x, y].Count == count)
                        {
                            int c = matches[x, y].First();
                            needed.Add(c);
                            Console.WriteLine($"{c}:{(char)c}");

                            for (int xx = 0; xx < LocalX(c); xx++)
                            for (int yy = 0; yy < LocalY(c); yy++)
                            {
                                if (m_charMaps[c][xx, yy])
                                {
                                    matches[xx, yy] = null;
                                }
                            }
                        }
                    }
                }
            }

            for (int i = MinChar; i < MaxChar; i++)
            {
                if (m_charMaps[i] != null && !needed.Contains(i))
                {
                    m_charMaps[i] = null;
                }
            }
        }

        private int PickFromCountIndex(int count)
        {
            int min = default;
            int max = m_counts.Length - 1;

            while ((min < max) && (min >= default(int)) && (max < m_counts.Length))
            {
                int mid = (max + min) / 2;
                int var = m_counts[mid].Count;
                if (var == count)
                {
                    // If we find the value just return the charter that goes with it
                    return mid;
                }
                if (var > count)
                {
                    // The value was more than what we want, so move to look in the lower part
                    max = mid - 1;
                }
                else
                {
                    // The value was less than what we want, so move up
                    min = mid + 1;
                }
            }

            int minDif;
            (min, minDif) = GetDiff(min, count);

            int maxDif;
            (max, maxDif) = GetDiff(max, count); 
            
            return minDif < maxDif ? min : max;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (int Index, int Dif) GetDiff(int index, int count)
        {
            index = Math.Min(Math.Max(index, default), m_counts.Length - 1);
            int dif = count - m_counts[index].Count;
            return (index, dif < default(int) ? -dif : dif);
        }

        private (bool[,], int) ComputeMapForChar(HashSet<string> visited, Font font, int charIndex, int size, float penWidth)
        {
            using (Image<Rgb24> image = new Image<Rgb24>(size, size))
            {
                Utilities.DrawChar(image, (char)charIndex, x: 1, y: 1, font, new Rectangle(1, 1, size, size), new SolidBrush(Color.White), Pens.Solid(Color.White, penWidth));

                Rgb24[] pixelArray = image.GetPixelSpan().ToArray();
                int localMaxX = default;
                int localMaxY = default;
                for (int y = default; y < image.Height; y++)
                {
                    int yValue = y * image.Width;
                    for (int x = default; x < image.Width; x++)
                    {
                        if (!pixelArray[yValue + x].IsBlack())
                        {
                            localMaxY = y;
                            if (x > localMaxX)
                            {
                                localMaxX = x;
                            }
                        }
                    }
                }

                string hashValue = string.Empty;
                uint current = default;
                int length = default;
                int count = default;
                var charMap = new bool[localMaxX+1, localMaxY+1];

                for (int y = default; y < charMap.GetLength(1); y++)
                {
                    int yValue = y * image.Width;
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

                        if (!pixelArray[yValue + x].IsBlack())
                        {
                            count++;
                            current |= 1;
                            charMap[x, y] = true;
                        }
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
        }

        private bool[][,] m_charMaps = new bool[MaxChar][,]; // First index is which char, the that is followed by (column, row)

        private (int Count, int Char)[] m_counts; // maps counts of pixes to a char (right now that char is last one that found that has that count);

        public const int MinChar = 32; // Space (skip all the non-printable ones)

        // This here might be reason to keep these 'chars' as ints, doing so would allow up to include char 255 and not overflow in for loops, but the places that are using this appear to be doing so with <
        public const int MaxChar = unchecked((byte)(~default(byte)));
    }
}