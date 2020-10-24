using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Ascii3dEngine
{
    public static class ColorUtilities
    {
        static ColorUtilities()
        {
            Dictionary<string, Rgb24> namedColors = typeof(Color)
                .GetFields(BindingFlags.Public | BindingFlags.Static)               // Get all the public Static Fields
                .Where(f => f.FieldType == typeof(Color) && f.IsInitOnly)           // We only want the Readonly Color ones
                .ToDictionary(f => f.Name,                                          // Map their Name to the value in a Dictionary that ignores case
                              f => ((Color)f.GetValue(default)).ToPixel<Rgb24>(),   // We want the RGB values
                              StringComparer.OrdinalIgnoreCase);

            if (!namedColors.ContainsKey(ConsoleColor.DarkYellow.ToString()))
            {
                // it looks like they have don't have Dark Yellow, so just throw Dark Goldenrod in there...
                // Without this we find like 10147, with a max difference of 8, with it we find 11771 (and addition of like 16%) and max difference of 7 (and a reduction of like 13%)
                // But from looking at the ColorChat and look from 50,50,50 to the origin, there are two distinked yellow lines
                // The others (green, cyan, blue, magenta and red) have a "Dark version" that overlaps so we need a little work picking a better match
                // With this change it brings the number of unique colors to 11576
                Rgb24 yellow = namedColors[ConsoleColor.Yellow.ToString()];
                double ration = (ComputeColorRation(namedColors, ConsoleColor.Magenta) + ComputeColorRation(namedColors, ConsoleColor.Cyan)) / 2.0;
                Rgb24 darkYellow = new Rgb24(
                    (byte)((double)(yellow.R) * ration),
                    (byte)((double)(yellow.G) * ration),
                    (byte)((double)(yellow.B) * ration));
                namedColors.Add(ConsoleColor.DarkYellow.ToString(), darkYellow);
            }

            s_allConsoleColors.AddRange(Enum.GetValues(typeof(ConsoleColor)).OfType<ConsoleColor>());

            s_consoleColors = s_allConsoleColors
                .Select(x => namedColors[x.ToString()])
                .ToArray();

            // So we can look at matching a color as looking through all the colors that we can make to find the one that matches the best.
            // We can make colors by selecting two console colors, and we can "mix" them by selecting a character
            // the more pixels the character uses, the more of the foreground color will be shown
            // So we are effectively look a version of the Nearest neighbor problem (https://en.wikipedia.org/wiki/Nearest_neighbor_search)
            // Our color componets R, G, B (0-255) will be our X, Y, Z cordenates.
            //
            // This can be a little problematic exterminations shows we can make some 11K unique colors.
            //
            // One thing to note is that the colors that we can create are NOT evenly distributed in our Color space
            // They are all spread out alone lines between the two Forground/Background colors.
            //
            // So maybe we can do better...
            // We should be able to search by finding the line that is closest to
            // https://www.youtube.com/watch?v=g2h3H0FkLjA
            // We have two colors (Foreground and Background) and we can express them as
            // r(t) = Background + t*(Foreground - Background)
            // Here r(t) will be the point on that line
            // r(t) = a + t*v
            // a will be our starting point (or Background) and v will be vector from Background to Forground
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
            // b will be vector from our target point to the point c (the closest point on the line, which will be be r(t) when t lines up correctly)
            // b = c - p  (we are going to want the length of this vector)
            // b = r(t) - p
            //    | Background.R + t * (Foreground.R - Background.R) - target.R |
            //    | Background.G + t * (Foreground.G - Background.G) - target.G |
            //    | Background.B + t * (Foreground.B - Background.B) - target.B |
            //
            // v X b = 0 (b/c a and b will be predictable )
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
            // t = --------------------
            //     (vR^2 + vG^2 + vB^2)
            //
            // We are going to compute these bunch so lets cache them
            // 
            //  t = ((stuff WITH target) + (stuff withOUT target))/(OTHER stuff withOU target)
            //  (stuff WITH target)          = vR * target.R + vG * target.G + vB * target.B
            //  (stuff withOUT target)       = - (vR * Background.R + vG * Background.G + vB * Background.B)
            //  (OTHER stuff withOUT target) = (vR^2 + vG^2 + vB^2)

            s_cachedDenominators = new double[s_consoleColors.Length, s_consoleColors.Length];
            s_cachedStaticNumeratorDenominators = new double[s_consoleColors.Length, s_consoleColors.Length];
            for (int i = 0; i < s_consoleColors.Length; i++)
            {
                // this will be our background color
                Rgb24 p1 = s_consoleColors[i];
                int p1R = p1.R;
                int p1G = p1.G;
                int p1B = p1.B;
                for (int j = 0; j < s_consoleColors.Length; j++)
                {
                    if (i != j)
                    {
                        // this will be our foreground color
                        Rgb24 p2 = s_consoleColors[j];

                        int vR = p2.R - p1R;
                        int vG = p2.G - p1G;
                        int vB = p2.B - p1B;

                        s_cachedStaticNumeratorDenominators[i, j] = -((vR * p1R) + (vG * p1G) + (vB * p1B));
                        s_cachedDenominators[i, j] = ((vR * vR) + (vG * vG) + (vB * vB));
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rgb24 NamedColor(ConsoleColor color) => s_consoleColors[(int)color];

        public static IEnumerable<ConsoleColor> ConsoleColors => s_allConsoleColors;
        
        public static (Char Character, ConsoleColor Foreground, ConsoleColor Background, Rgb24 Result) BestMatch(CharMap map, Rgb24 target)
        {
            // So this looks rather complicated, did save us anything?
            // See BruteForce...

            int tR = target.R;
            int tG = target.G;
            int tB = target.B;

            // We multiply this by values between 0 and 1, so keep it as a double
            double maxPixels = (double)(map.MaxX * map.MaxY);

            double resultDistance = double.MaxValue;
            char character = default;
            ConsoleColor foreground = default;
            ConsoleColor background = default;
            Rgb24 result = default;

            double[] pointDistances = new double[s_consoleColors.Length];
            bool[] pointReady = new bool[s_consoleColors.Length];
            double[] lineDistance = new double[s_consoleColors.Length];
            for (int i = 0; i < pointDistances.Length; i++)
            {
                pointDistances[i] = Difference(s_consoleColors[i], target);
                pointReady[i] = true;
            }

            // This loop will happen s_consoleColors.Length (16) times
            while(true)
            {
                //
                // by picking the one "point" that is closest, I think we can more quickly find the best fit
                // and that should allow us to bail out soon on later ones. 
                int selectedIndex = -1;
                double minDistance = double.MaxValue;
                for (int i = 0; i < pointDistances.Length; i++)
                {
                    if (pointReady[i] && pointDistances[i] < minDistance)
                    {
                        selectedIndex = i;
                        minDistance = pointDistances[i];
                    }
                }

                if (selectedIndex < 0)
                {
                    break;
                }

                // this will be our candidate background color
                Rgb24 selected = s_consoleColors[selectedIndex];
                double p1R = selected.R;
                double p1G = selected.G;
                double p1B = selected.B;

                // mark this one used, this is what keeps us to at most 16 loops
                pointReady[selectedIndex] = false;

                // I don't think there is any benifits to try to pick the closest (or even farthest) second color
                // This loop does "try" all the colors, but we skip those already processed
                for (int secondIndex = 0; secondIndex < pointReady.Length; secondIndex++)
                {
                    if (pointReady[secondIndex])
                    {
                        // this will be our cadidate foreground color
                        Rgb24 second = s_consoleColors[secondIndex];
                        double p2R = second.R;
                        double p2G = second.G;
                        double p2B = second.B;

                        double vR = p2R - p1R;
                        double vG = p2G - p1G;
                        double vB = p2B - p1B;

                        // We now have two "point" now we need to compute the distance between this line and the target
                        // https://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line
                        // and
                        // https://www.youtube.com/watch?v=g2h3H0FkLjA
                        // Plus all that stuff we cached
                        // Remember
                        //  (stuff WITH target)          = vR * target.R + vG * target.G + vB * target.B
                        double numerator = (vR * tR) + (vG * tG) + (vB * tB) + s_cachedStaticNumeratorDenominators[selectedIndex, secondIndex];

                        // we are about to compute t, before we do there is some filtering at we can do that point
                        // if t = 0, the background color is the target
                        // if t = 1, the foreground color is the target
                        // if t < 0, that means the targe color is on the wrong side of the background color, there is no amount of foreground color we could replace to get to the target
                        // the value we cached for the denominator will always be postive (sum of natural number squares)
                        // so if the numerator is negative, then t will be as well
                        if (numerator < 0)
                        {
                            continue;
                        }

                        double t = numerator / s_cachedDenominators[selectedIndex, secondIndex];

                        // We also know that the selected point is closer to the target then second point, that means t should really be <= 0.5
                        // We hae done a lot of crazy math at this point, let check to make sure
                        if (t > 0.5)
                        {
                            throw new Exception($@"Boom! t >0.5
                            {nameof(t)}:{t}
                            {nameof(target)}:{target}
                            {nameof(selected)}:{selectedIndex} {selected}
                            {nameof(second)}:{secondIndex} {second}
                            {nameof(s_cachedStaticNumeratorDenominators)}:{s_cachedStaticNumeratorDenominators[selectedIndex, secondIndex]}
                            {nameof(s_cachedDenominators)}:{s_cachedDenominators[selectedIndex, secondIndex]}");
                        }

                        double differenceFromLineR = p1R + (t * vR) - tR;
                        double differenceFromLineG = p1G + (t * vG) - tG;
                        double differenceFromLineB = p1B + (t * vB) - tB;

                        double distanceToLine = Math.Sqrt((differenceFromLineR * differenceFromLineR) + (differenceFromLineG * differenceFromLineG) + (differenceFromLineB * differenceFromLineB));

                        if (distanceToLine < resultDistance)
                        {
                            // the intersection will happen at r(t)
                            // But we really want to translate that to into the how far are we from the Background color and how close are we to Foreground color
                            // and lucky that is exactly what t is :) Remember r(t) = Background + t*(Foreground - Background)
                            // so r(0) = Background, r(1) = Background + Foreground - Background = Foreground

                            int count = (int)Math.Round(t * maxPixels);

                            // We are going to make an assumption here.  Basically the grayscale generated from ImageProcessing project (that donated its line fitting algorithms)
                            // Show none of character have more black pixels then white ones (aks the filled in blocks █, ascii 9608) are included
                            // This means our options would go count = 0 => all background/no foreground, then as count in creases we would get more and more foreground.
                            // Then at t = 0.5 we would flip.
                            // We know that Background is closer to tharget, so r(t) needs to be closer to Background then Foreground (basically that t is guaranteed to <= 0.5).
                            // The up-shot of this, is that we still don't need ImageProcessing's ability to also check "inverses"
                            (char c, int numberOfPixels) = map.PickFromCountWithCount(count);

                            // Now that we know how many pixes cover the best match, we can figure out where that is along our line
                            double charsT = numberOfPixels / maxPixels;

                            // we can compute this using our r(t) equation
                            Rgb24 currentColor = new Rgb24(
                                ColorValue(p1R, charsT, vR),
                                ColorValue(p1G, charsT, vG),
                                ColorValue(p1B, charsT, vB));

                            double pointDifference = Difference(target, currentColor);
                            if (pointDifference < resultDistance)
                            {
                                character = c;
                                background = (ConsoleColor)selectedIndex;
                                foreground = (ConsoleColor)secondIndex;
                                resultDistance = pointDifference;
                                result = currentColor;
                            }
                        }
                    }
                }
            }

            return (character, foreground, background, result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte ColorValue(double p, double charsT, double v)
            => (byte)(Math.Min(Math.Max(0, p + charsT * v), MaxByte));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Difference(Rgb24 c1, Rgb24 c2)
        {
            double dR = (int)c1.R - (int)c2.R;
            double dG = (int)c1.G - (int)c2.G;
            double dB = (int)c1.B - (int)c2.B;
            return Math.Sqrt((dR * dR) + (dG * dG) + (dB * dB));
        }

        private static double ComputeColorRation(Dictionary<string, Rgb24> namedColors, ConsoleColor color)
        {
            Rgb24 baseColor = namedColors[color.ToString()];
            Rgb24 darkColor = namedColors[$"Dark{color.ToString()}"];

            int sumOfBase = (int)baseColor.R + (int)baseColor.G + (int)baseColor.B;
            int sumOfDark = (int)darkColor.R + (int)darkColor.G + (int)darkColor.B;

            return ((double)sumOfDark) / ((double)sumOfBase);
        }

        public const double MaxByte = (double)byte.MaxValue;

        private readonly static Rgb24[] s_consoleColors;
        private readonly static double[,] s_cachedDenominators;
        private readonly static double[,] s_cachedStaticNumeratorDenominators;
        private readonly static List<ConsoleColor> s_allConsoleColors = new List<ConsoleColor>();

        public static class BruteForce
        {
            // Running this on my mac
            // ~/Projects/Ascii3dEngine > dotnet run -c Release
            // SetMap: 00:00:00.0128667
            // Create Test Cases: 00:00:00.0030840
            // Brute Force: 00:00:04.2634756
            // Crazy: 00:00:00.3640753
            // Distance: 00:00:04.5484271
            // max:2.4888430668773074
            // sum:9747.250866513969
            // Avg:0.09747250866513968
            // ~/Projects/Ascii3dEngine >
            // So we ca see that the crazy method only take 8% of time the brute force, there are some differences.
            // To be clear the two methods might return difference values that equivlent, but the differences recoded is additional variances of the target
            // I'm fairly sure that is caused by rounding

            /*
            From Last Run
maxError:113.14592347937243
sumError:1933964.7192507342
Avg Error: 19.339647192507343
Unique Test cases: 99716 or 100000 = (99%)
maxCrazy:83.3186653757728
sumCrazy:109479.43344999844
Crazy Avg: 1.0947943344999844
0|83.3186653757728|109479.43344999844|1.0947943344999844
1|110.09995458672996|1560046.8606199613|15.600468606199613
2|110.09995458672996|1558739.2533160916|15.587392533160916
4|110.09995458672996|1556196.9155648607|15.561969155648606
8|110.09995458672996|1550555.0971217533|15.505550971217533
16|110.09995458672996|1542414.3356589593|15.424143356589594
32|110.09995458672996|1527478.7158305217|15.274787158305216
64|110.09995458672996|1479372.966554327|14.79372966554327
128|110.09995458672996|1423911.7807106886|14.239117807106886
256|110.09995458672996|1345175.9031586887|13.451759031586887
512|110.09995458672996|1225802.8164356365|12.258028164356364
1024|110.09995458672996|1172918.880417711|11.729188804177111
2048|90.50966799187809|343787.1898508284|3.4378718985082837
4096|90.50966799187809|305900.29516550875|3.0590029516550876
8192|90.50966799187809|305900.29516550875|3.0590029516550876
16384|0|0|0
            */

            public static void AccuracyReport()
            {
                CharMap map = StaticColorValidationData.Map;

                (IReadOnlyDictionary<Rgb24, Rgb24> bestMatches, double maxError, double sumError) = StaticColorValidationData.BestMatches;
                int colorsToCheck = StaticColorValidationData.TestColors.Length;

                Console.WriteLine($"{nameof(maxError)}:{maxError}");
                Console.WriteLine($"{nameof(sumError)}:{sumError}");
                Console.WriteLine($"Avg Error: {sumError / (double)colorsToCheck}");
                Console.WriteLine($"Unique Test cases: {bestMatches.Count} or {colorsToCheck} = ({bestMatches.Count * 100 / colorsToCheck}%)");

                double maxCrazy = double.MinValue;
                double sumCrazy = 0;

                for (int i = 0; i < StaticColorValidationData.TestColors.Length; i++)
                {
                    var match = ColorUtilities.BestMatch(map, StaticColorValidationData.TestColors[i]).Result;
                    var brute = bestMatches[StaticColorValidationData.TestColors[i]];
                    double dif = Difference(match, brute);
                    maxCrazy = Math.Max(maxCrazy, dif);
                    sumCrazy += dif;
                }

                Console.WriteLine($"{nameof(maxCrazy)}:{maxCrazy}");
                Console.WriteLine($"{nameof(sumCrazy)}:{sumCrazy}");
                Console.WriteLine($"Crazy Avg: {sumCrazy / (double)colorsToCheck}");

                Console.WriteLine($"{0}|{maxCrazy}|{sumCrazy}|{sumCrazy / (double)colorsToCheck}");

                for(int maxChildren = 1; ; maxChildren *= 2)
                {
                    ColorOctree octree = StaticColorValidationData.CreateOctree(maxChildren);
                    var counts = octree.Count();

                    double maxOctree = double.MinValue;
                    double sumOctree = 0;
                    for (int i = 0; i < StaticColorValidationData.TestColors.Length; i++)
                    {
                        var match = octree.BestMatch(StaticColorValidationData.TestColors[i]).Result;
                        var brute = bestMatches[StaticColorValidationData.TestColors[i]];
                        double dif = Difference(match, brute);
                        maxOctree = Math.Max(maxOctree, dif);
                        sumOctree += dif;
                    }

                    Console.WriteLine($"{maxChildren}|{maxOctree}|{sumOctree}|{sumOctree / (double)colorsToCheck}");

                    if (counts.NodesWithLeafs == 1)
                    {
                        break;
                    }
                }
            }
        }
    }
}