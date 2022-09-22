using System.Runtime.CompilerServices;
using SixLabors.ImageSharp.PixelFormats;

namespace Ascii3dEngine.Engine
{
    public static class StaticColorValidationData
    {
        static StaticColorValidationData()
        {
            double max = Map.MaxX * Map.MaxY;
            var colors = new HashSet<Rgb24>();

            foreach (var background in ColorUtilities.ConsoleColors)
            {
                var selected = ColorUtilities.NamedColor(background);
                foreach (var foreground in ColorUtilities.ConsoleColors)
                {
                    var second = ColorUtilities.NamedColor(foreground);
                    foreach (var count in Map.Counts)
                    {
                        double countDouble = count.Count;
                        double uncovered = max - countDouble;
                        var nc = new Rgb24(
                            ColorValue(second.R, countDouble, selected.R, uncovered, max),
                            ColorValue(second.G, countDouble, selected.G, uncovered, max),
                            ColorValue(second.B, countDouble, selected.B, uncovered, max));

                        if (colors.Add(nc))
                        {
                            ((List<(char Character, ConsoleColor Foreground, ConsoleColor Background, Rgb24 Color)>)s_paletteItems)
                                .Add(((char)count.Char, foreground, background, nc));
                        }
                    }
                }
            }

            var r = new Random(5);  // We don't really need this to be random, repeatable is handy for trouble shooting :)
            TestColors = new Rgb24[c_colorsToCheck];
            for (int i = 0; i < TestColors.Length; i++)
            {
                TestColors[i] = new Rgb24(
                    // Need +1 here because we get numbers in the range [0-max)
                    (byte)r.Next((int)byte.MaxValue + 1),
                    (byte)r.Next((int)byte.MaxValue + 1),
                    (byte)r.Next((int)byte.MaxValue + 1));
            }
        }

        public static ColorOctree CreateOctree(int maxChildrenCount)
        {
            var result = new ColorOctree(maxChildrenCount);
            foreach ((var character, var foreground, var background, var color) in s_paletteItems)
            {
                result.Add(new ColorOctreeLeaf(foreground, background, character, color));
            }
            return result;
        }

        public static (IReadOnlyDictionary<Rgb24, Rgb24> matches, double MaxError, double SumError) BestMatches
        {
            get
            {
                // We are going to compute the same value, so there is no need for a lock or any thing
                if (s_bestMatches == null)
                {
                    double maxError = double.MinValue;
                    double sumError = 0;

                    var bestMatches = new Dictionary<Rgb24, Rgb24>();
                    for (int i = 0; i < TestColors.Length; i++)
                    {
                        // there can be duplicates in our test data.
                        if (!bestMatches.TryGetValue(TestColors[i], out Rgb24 bestMatch))
                        {
                            bestMatch = BestMatch(TestColors[i]).Result;
                            bestMatches.Add(TestColors[i], bestMatch);
                        }

                        double error = ColorUtilities.Difference(bestMatch, TestColors[i]);
                        maxError = Math.Max(maxError, error);
                        sumError += error;
                    }
                    s_maxError = maxError;
                    s_sumError = sumError;
                    s_bestMatches = bestMatches;
                }
                return (s_bestMatches, s_maxError, s_sumError);
            }
        }

        public static readonly Rgb24[] TestColors;
        public static readonly CharMap Map = new();

        private static double s_maxError;
        private static double s_sumError;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]  // "t<0.5"
        private static byte ColorValue(byte foreground, double covered, byte background, double uncovered, double max)
            => (byte)Math.Round((((double)foreground * covered) + ((double)background * uncovered))/max);

        public static (char Character, ConsoleColor Foreground, ConsoleColor Background, Rgb24 Result) BestMatch(Rgb24 target)
        {
            int resultDistanceProxy = int.MaxValue;
            char character = default;
            ConsoleColor foreground = default;
            ConsoleColor background = default;
            Rgb24 result = default;

            foreach (var item in s_paletteItems)
            {
                int distanceProxy = ColorUtilities.DifferenceProxy(target, item.Color);
                if (distanceProxy < resultDistanceProxy)
                {
                    character = item.Character;
                    foreground = item.Foreground;
                    background = item.Background;
                    resultDistanceProxy = distanceProxy;
                    result = item.Color;
                }
            }

            return (character, foreground, background, result);
        }

        private const int c_colorsToCheck = 100000;

        private static Dictionary<Rgb24, Rgb24>? s_bestMatches;
        
        private static readonly IEnumerable<(Char Character, ConsoleColor Foreground, ConsoleColor Background, Rgb24 Color)> s_paletteItems 
            = new List<(char Character, ConsoleColor Foreground, ConsoleColor Background, Rgb24 Color)>();
    }
}