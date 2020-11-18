using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
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

            s_allConsoleColors.AddRange(
                Enum.GetValues(typeof(ConsoleColor))
                    .OfType<ConsoleColor>()
                    .OrderBy(x => x));

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

        // These are only set when PROFILECOLOR is true
        public static int CountsCalls = 0;
        public static int CountsTrueLoop = 0;
        public static int CountsBackgrounds = 0;
        public static int CountsForegrounds = 0;
        public static int CountsComputeT = 0;
        public static int CountsComputeTGood = 0;
        public static int CountsChangeMatch = 0;

        public static (Char Character, ConsoleColor Foreground, ConsoleColor Background, Rgb24 Result) BestMatch(CharMap map, Rgb24 target)
        {
#if (PROFILECOLOR)
            CountsCalls++;
#endif

            // So this looks rather complicated, did save us anything?
            // See BruteForce and ColorMatchingBenchmarks

            int tR = target.R;
            int tG = target.G;
            int tB = target.B;

#if (!GENERATECOUNTS)
            int colorIndex = ColorIndex(target);
#endif

            // Instead of computing and comparing true distance, we can compute just the proxy, and avoid using Math.Sqrt, this is true even for a Crazy geometry approach
            // The benchmark for this change is not show as large of an improvement as I was expecting for searching for 100000 random colors
            // |   Method |     Mean |   Error |  StdDev | Ratio | RatioSD |
            // |--------- |---------:|--------:|--------:|------:|--------:|
            // | Baseline | 310.4 ms | 2.65 ms | 2.21 ms |  1.00 |    0.00 |
            // | TestFlag | 306.1 ms | 5.93 ms | 6.59 ms |  0.99 |    0.02 | 
            // Baseline was the old code without the optimization, but the TestFlag version did have to do extra type conversions form double to int
            // But running less code, especially complicated is always faster
            //
            // One thing to note, this did introduce an extra point where we round, and that does affect the accuracy
            //                           |            max |              sum |              avg
            // Without the optimization 0|83.3186653757728|109479.43344999844|1.0947943344999844
            // With                     0|83.3186653757728|111340.83064227670|1.1134083064227671
            // Being more careful to limit that impact (and do a little more work with doubles) was able to correct that problem and after thinking critically about the rounding
            // was able to make a small change to even further improve the accuracy
            //                          0|83.3186653757728|108416.63811329169|1.084166381132917
            int resultDistanceProxy = int.MaxValue;

            char character = default;
            ConsoleColor foreground = default;
            ConsoleColor background = default;
            Rgb24 result = default;

            // These are small (16 in length) so they should fit on the stack.
            // I parameterized them, and ran a benchmark, and found a 3% saves over searching 100000 random colors
            // |   Method |     Mean |   Error |  StdDev |   Median | Ratio | RatioSD |
            // |--------- |---------:|--------:|--------:|---------:|------:|--------:|
            // | Baseline | 344.4 ms | 5.49 ms | 4.58 ms | 345.3 ms |  1.00 |    0.00 |
            // | TestFlag | 336.2 ms | 6.58 ms | 8.55 ms | 331.1 ms |  0.97 |    0.03 |
            // TestFlag was with using the Spans.
            // But that savings 8.2 ms is within the 8.55 ms of the StdDev on the second run... so that does put it within the range of just noise
            Span<int> pointDistances = stackalloc int[s_consoleColors.Length];
            Span<bool> pointReady = stackalloc bool[s_consoleColors.Length];

            for (int i = 0; i < pointDistances.Length; i++)
            {
#if (!GENERATECOUNTS)
                // If we are ignoring this color, we don't even need to bother computing the distance or marking it ready to use
                if (s_backgroundsToSkip[colorIndex, i] && s_foregroundsToSkip[colorIndex, i])
                {
                    pointDistances[i] = int.MaxValue;
                    pointReady[i] = false;
                    continue;
                }
#endif
                pointDistances[i] = DifferenceProxy(s_consoleColors[i], target);
                pointReady[i] = true;
            }

            // This loop will happen s_consoleColors.Length (16) times
            while(true)
            {
#if (PROFILECOLOR)
                CountsTrueLoop++;
#endif
                //
                // by picking the one "point" that is closest, I think we can more quickly find the best fit
                // and that should allow us to bail out soon on later ones. 
                //
                // I went back in and checked this assumption with benchmark searching 100000 random colors
                // |   Method |     Mean |   Error |  StdDev | Ratio | RatioSD |
                // |--------- |---------:|--------:|--------:|------:|--------:|
                // | Baseline | 339.8 ms | 6.65 ms | 7.11 ms |  1.00 |    0.00 |
                // | TestFlag | 376.3 ms | 6.68 ms | 5.58 ms |  1.11 |    0.02 |
                // TestFlag was with this optimization removed, it appears to be about 10% slow, that differences was bigger than I original thought it would be
                // But not only does this allow us narrow resultDistance as fast as possible and inturn check less stuff, it also means we throw point out once we test them
                // so instead of testing n^2 (16*16=256) lines, we really test C(n,k) or C(16,2) 16!/(n!(n-k!)) 20922789888000/(2 * 87178291200) = 120
                // And that is where the big savings come in.
                //
                // One thing that I was not expecting, is that this change appear to have affected the accuracy
                //                        |             max |              sum |              avg
                // Optimization in place 0|83.31866537577280|109479.43344999844|1.0947943344999844
                //               removed 0|85.98837130682264|109670.83507204286|1.0967083507204285
                // So I'm not sure how removing the optimization made it more inaccurate
                //
                // I looked into changing using a sort... but these are really short and it does not look like it it will help
                int selectedIndex = -1;
                int minDistance = int.MaxValue;
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

                // mark this one used, this is what keeps us to at most 16 loops
                pointReady[selectedIndex] = false;

#if (!GENERATECOUNTS)
                if (s_backgroundsToSkip[colorIndex, selectedIndex])
                {
                    continue;
                }
#endif

#if (PROFILECOLOR)
                CountsBackgrounds++;
#endif

                // this will be our candidate background color
                Rgb24 selected = s_consoleColors[selectedIndex];
                double p1R = selected.R;
                double p1G = selected.G;
                double p1B = selected.B;

                // I don't think there is any benifits to try to pick the closest (or even farthest) second color
                // This loop does "try" all the colors, but we skip those already processed
                for (int secondIndex = 0; secondIndex < pointReady.Length; secondIndex++)
                {
                    if (pointReady[secondIndex])
                    {

#if (!GENERATECOUNTS)
                        if (s_foregroundsToSkip[colorIndex, secondIndex])
                        {
                            continue;
                        }
#endif

#if (PROFILECOLOR)
                        CountsForegrounds++;
#endif

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

                        // we are about to compute t, before we do that there is some filtering at we can do that point
                        // if t = 0, the background color is the target
                        // if t = 1, the foreground color is the target
                        // if t < 0, that means the targe color is on the wrong side of the background color, there is no amount of foreground color we could replace to get to the target
                        // the value we cached for the denominator will always be postive (sum of natural number squares)
                        // so if the numerator is negative, then t will be as well
                        if (numerator < 0)
                        {
                            continue;
                        }

#if (PROFILECOLOR)
                        CountsComputeT++;
#endif

                        double t = numerator / s_cachedDenominators[selectedIndex, secondIndex];

                        // We also know that if the selected point is closer to the target than second point, that means t should really be <= 0.5
                        // We have done a lot of crazy math at this point, let check to make sure.  I'll mark places that use this assumptions with "t<0.5"
                        if (t > 0.5)
                        {
                            throw new Exception($@"Boom! t >0.5
                            {nameof(t)}:{t}
                            {nameof(target)}:{target}
                            {nameof(selected)}:{selectedIndex} {(ConsoleColor)selectedIndex} {selected}
                            {nameof(second)}:{secondIndex} {(ConsoleColor)secondIndex} {second}
                            {nameof(numerator)}:{numerator}
                            {nameof(s_cachedStaticNumeratorDenominators)}:{s_cachedStaticNumeratorDenominators[selectedIndex, secondIndex]}
                            {nameof(s_cachedDenominators)}:{s_cachedDenominators[selectedIndex, secondIndex]}
                            {nameof(vR)}:{vR}
                            {nameof(vG)}:{vG}
                            {nameof(vB)}:{vB}");
                        }

                        //
                        // The Rounding here should have very little impact on the result b/c we don't use this in the finial calculation but we do use it to to eliminate things
                        // But it can have an effect.  And Example
                        // A = {whole number} + {a fraction close to but less than 0.5 }
                        // A^2 = {whole number}^2 + 2({whole number} * {a fraction close to but less than 0.5 }) + {a fraction close to but less than 0.5 }^2
                        // ((int)A)^2 = {whole number}^2
                        // A^2 - ((int)A)^2 = 2({whole number} * {a fraction close to but less than 0.5 }) + {a fraction close to but less than 0.5 }^2
                        // So the amount that we are off is affected by how big that whole number is
                        //
                        // int differenceFromLineR = (int)selected.R + (int)Math.Round(t * vR) - (int)target.R;
                        // int differenceFromLineG = (int)selected.G + (int)Math.Round(t * vG) - (int)target.G;
                        // int differenceFromLineB = (int)selected.B + (int)Math.Round(t * vB) - (int)target.B;
                        // Using ints the optimization checking our 100000 the avg off was 1.1134083064227671, leaving them as double a little longer keeps us to 1.0947943344999844
                        double differenceFromLineR = p1R + (t * vR) - tR;
                        double differenceFromLineG = p1G + (t * vG) - tG;
                        double differenceFromLineB = p1B + (t * vB) - tB;

                        // There is a rounding that is happening here and really not even rounding, truncation.  But this is unlikely to make a big differences
                        // the only time the routing here will make a difference is when the line we are checking is close the result distance AND the point we will choose on that line
                        // is right at that intersection point, but we can nullify that truncation error by simply checking lines that are atleast as close, they don't have be strictly closer
                        int distanceToLineProxy = (int)((differenceFromLineR * differenceFromLineR) + (differenceFromLineG * differenceFromLineG) + (differenceFromLineB * differenceFromLineB));

                        if (distanceToLineProxy <= resultDistanceProxy)
                        {
#if (PROFILECOLOR)
                            CountsComputeTGood++;
#endif

                            // the intersection will happen at r(t)
                            // But we really want to translate that into how far are we from the Background color and how close are we to Foreground color
                            // and lucky that is exactly what t is :) Remember r(t) = Background + t*(Foreground - Background)
                            // so r(0) = Background, r(1) = Background + Foreground - Background = Foreground
                            //
                            // We are going to make an assumption here.  Basically the grayscale generated from ImageProcessing project (that donated its line fitting algorithms)
                            // Showed none of character have more black pixels then white ones (aks the filled in blocks █, ascii 9608) are not included
                            // This means our options would go count = 0 => all background/no foreground, then as count in creases we would get more and more foreground.
                            // Then at t = 0.5 we would flip (there would be for foreground pixels then background).
                            // We know that Background is closer to tharget, so r(t) needs to be closer to Background than Foreground (basically that t is guaranteed to <= 0.5).
                            // The up-shot of this, is that we still don't need ImageProcessing's ability to also check "inverses"

                            (char c, double pixelRatio) = map.PickFromRatio(t);

                            // we can compute this using our r(t) equation
                            Rgb24 currentColor = new Rgb24(
                                ColorValue(selected.R, pixelRatio, vR),
                                ColorValue(selected.G, pixelRatio, vG),
                                ColorValue(selected.B, pixelRatio, vB));

                            int pointDifferenceProxy = DifferenceProxy(target, currentColor);
                            if (pointDifferenceProxy < resultDistanceProxy)
                            {
#if (PROFILECOLOR)
                                CountsChangeMatch++;
#endif

                                character = c;
                                background = (ConsoleColor)selectedIndex;
                                foreground = (ConsoleColor)secondIndex;
                                resultDistanceProxy = pointDifferenceProxy;
                                result = currentColor;
                            }
                        }
                    }
                }
            }

            return (character, foreground, background, result);
        }

        // You might think that you need Min/Max checks here,ut those are not necessary and here is why
        //   They would only have an effect when ColorValue we are choosing "close" those edges.
        //   If p is small (or even 0) then v will be positive (because v will point in the direction of Foreground, and we already decided that p, the Backround is small)
        //   The same holds true if p is large, then v will negative (since it points from our larget Background to smaller Foreground)
        //   "t<0.5"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte ColorValue(byte p, double pixelRatio, double v) 
            => (byte)(p + (byte)Math.Round(pixelRatio * v));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Difference(Rgb24 c1, Rgb24 c2) => Math.Sqrt(DifferenceProxy(c1, c2));

        /// <summary>
        /// This value is a "proxy" for difference between these too colors, it is cheaper to compute
        /// But retains the property if the DifferenceProxy(c1, c2) < DifferenceProxy(c1, c3) then Difference(c1) < Difference(c1), same goes for ==
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DifferenceProxy(Rgb24 c1, Rgb24 c2)
        {
            int dR = (int)c1.R - (int)c2.R;
            int dG = (int)c1.G - (int)c2.G;
            int dB = (int)c1.B - (int)c2.B;
            return (dR * dR) + (dG * dG) + (dB * dB);
        }

        // This method is used to partion our color space, in its current from it is more or less an even distribution
        // But I there may be other distributions that may work, we do need to keep computing it fast.static  But the most important
        // Attribute is this partitions the colors so the hight number of background (or pontentail foreground) colors can be ignored.
        // 
        // While testing this out out I enumerated all colors and collected profiling data to see how far we got for each attempt
        // Here is the data without optimization
        // CountsCalls       :   16777216
        // CountsBackgrounds :  268435456       16
        // CountsForegrounds : 2013265920      120
        // CountsComputeT    : 1518060147       90
        // CountsComputeTGood:   80687610        4
        // CountsChangeMatch :   77321761        4
        //
        // And here are the results after breaking Read, Green, Blue down the middel
        // CountsCalls       :   16777216
        // CountsBackgrounds :  182368897       10
        // CountsForegrounds : 1729218160      103
        // CountsComputeT    : 1284964529       76
        // CountsComputeTGood:   80670346        4
        // CountsChangeMatch :   77319143        4
        //
        // The Call count remains the same b/c both test all colores, we reduce the Backround colors to ~67%
        // And forground and times that we compute T to ~85%
        // And from benchmarcking it
        // |        Method |                             Arguments | N |     Mean |   Error |  StdDev |
        // |-------------- |-------------------------------------- |-- |---------:|--------:|--------:|
        // | FindAllColors | /p:GENERATECOUNTS=true,/t:Clean;Build | 0 | 346.9 ms | 6.21 ms | 8.29 ms |
        // | FindAllColors |                        /t:Clean;Build | 0 | 294.1 ms | 3.73 ms | 2.91 ms |
        // GENERATECOUNTS=true means the optimization is not in place.
        // So with it there it reduces the run time by of testing 100000 colors to about ~85%
        //
        // Changing this to divide each color demention into 4 sections instead of 2 yields even more savings
        // CountsCalls       :   16777216
        // CountsBackgrounds :   88866816        5
        // CountsForegrounds : 1031028758       61
        // CountsComputeT    :  774639230       46
        // CountsComputeTGood:   79864581        4
        // CountsChangeMatch :   76591270        4
        // |        Method |                       Arguments | N |     Mean |   Error |  StdDev |
        // |-------------- |-------------------------------- |-- |---------:|--------:|--------:|
        // | FindAllColors | /p:TESTFLAG=true,/t:Clean;Build | 0 | 214.3 ms | 4.13 ms | 3.86 ms |
        // | FindAllColors |                  /t:Clean;Build | 0 | 297.9 ms | 5.68 ms | 6.08 ms |
        // TESTFLAG=true means spiting by 4, without it means spliting by 2
        // There it reduced to about ~71%.  But this change is not FREE... s_backgroundsToSkip grows.
        // Before it (2 * 2 * 2) or 8 x 16 bools
        // Now it is (4 * 4 * 4) or 64 x 16 bools
        // But that array is static 
        // Comparing accors is not really valid by since 294.1 and 297.9 so close you could say this about a 40% savings overall
        //
        // From reviewing the counts it looks like this new split also has a bunch of zero for the goregrounds, so we should be able
        // to apply the same logic and get some more savings.
        // And mocking that up we get this
        // CountsCalls       :   16777216
        // CountsBackgrounds :   88866816        5
        // CountsForegrounds :  756116879       45
        // CountsComputeT    :  602294853       35
        // CountsComputeTGood:   73016770        4
        // CountsChangeMatch :   69925105        4
        // We do reduce the CountForegrounds and CountsComputeT to about ~75%
        //
        // |        Method |                       Arguments | N |     Mean |   Error |  StdDev |
        // |-------------- |-------------------------------- |-- |---------:|--------:|--------:|
        // | FindAllColors | /p:TESTFLAG=true,/t:Clean;Build | 0 | 195.9 ms | 3.88 ms | 4.77 ms |
        // | FindAllColors |                  /t:Clean;Build | 0 | 236.2 ms | 3.40 ms | 3.18 ms |
        // TESTFLAG=true means we have this additional optimization in place
        // This shows we reduce the the run time of checking 100000 colors to 80% and just to compare this without any of this optimization
        // |        Method |                             Arguments | N |     Mean |   Error |  StdDev |
        // |-------------- |-------------------------------------- |-- |---------:|--------:|--------:|
        // | FindAllColors | /p:GENERATECOUNTS=true,/t:Clean;Build | 0 | 329.1 ms | 3.20 ms | 2.67 ms |
        // | FindAllColors |                        /t:Clean;Build | 0 | 196.1 ms | 3.74 ms | 4.16 ms |
        // GENERATECOUNTS=true means all of this is disabled
        // So this reduces the run time just about 60%
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ColorIndex(Rgb24 color) => (color.R >> 6)
                                                  | ((color.G >> 6) << 2)
                                                  | ((color.B >> 6) << 4);

        public static int MaxColorIndex => ColorIndex(new Rgb24(Byte.MaxValue, Byte.MaxValue, Byte.MaxValue));

        private static double ComputeColorRation(Dictionary<string, Rgb24> namedColors, ConsoleColor color)
        {
            Rgb24 baseColor = namedColors[color.ToString()];
            Rgb24 darkColor = namedColors[$"Dark{color.ToString()}"];

            int sumOfBase = (int)baseColor.R + (int)baseColor.G + (int)baseColor.B;
            int sumOfDark = (int)darkColor.R + (int)darkColor.G + (int)darkColor.B;

            return ((double)sumOfDark) / ((double)sumOfBase);
        }

        private readonly static Rgb24[] s_consoleColors;
        private readonly static double[,] s_cachedDenominators;
        private readonly static double[,] s_cachedStaticNumeratorDenominators;
        private readonly static List<ConsoleColor> s_allConsoleColors = new List<ConsoleColor>();

        // These Static array are computed bu running BruteForce.Counting() like so
        // dotnet build -c Release -t:"Clean;Build" -p:GENERATECOUNTS=true; dotnet run -c Release --no-build
        // It is a then a good idea to to do another clean build without GENERATECOUNTS set.
        // this represents which background colors can be ignored.  The first index the value determed by ColorIndex.
        // The second value is the just console color casted to int.
        // This works b/c for every region of our color space there are some colors that will never make a good background
        // color.  We could do something similar for foreground colors, but I have yet to find split that looks good 
        //
        // I should check to see how different fonts affects these...
        private readonly static bool[,] s_backgroundsToSkip = new bool[,] {
            {false, false, false, true, false, false, true, true, true, true, true, true, true, true, true, true},
            {false, true, false, true, false, false, true, true, true, true, true, true, true, true, true, true},
            {true, true, true, true, false, true, false, true, true, true, true, true, false, true, true, true},
            {true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true},
            {false, true, false, true, true, true, true, true, true, true, false, true, true, true, true, true},
            {false, false, false, false, false, true, false, false, true, true, false, true, true, true, true, true},
            {true, false, true, true, false, false, false, false, true, true, true, true, false, true, false, true},
            {true, true, true, true, false, false, false, false, true, true, true, true, false, true, false, true},
            {true, true, false, true, true, true, true, true, true, true, false, true, true, true, true, true},
            {true, false, false, false, true, true, false, false, true, true, false, true, true, true, false, true},
            {true, true, false, false, false, false, false, true, true, true, true, true, true, true, false, true},
            {true, true, true, true, false, true, false, true, true, true, true, true, true, true, false, true},
            {true, true, true, false, true, true, false, true, true, true, false, true, true, true, true, true},
            {true, true, true, false, true, true, false, true, true, true, false, true, true, true, false, true},
            {true, true, true, true, true, true, false, true, true, true, true, true, true, true, false, true},
            {true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true},
            {false, false, false, true, true, false, true, true, true, true, true, true, true, true, true, true},
            {false, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true},
            {true, true, false, true, false, false, false, false, true, true, true, true, false, false, true, true},
            {true, true, true, true, false, false, false, false, true, true, true, true, false, false, true, true},
            {false, false, false, false, false, true, false, false, true, true, false, true, true, true, true, true},
            {false, false, false, false, false, false, false, false, false, true, false, true, true, true, true, true},
            {true, false, true, true, false, false, false, false, false, true, false, true, false, false, false, true},
            {true, true, true, true, false, false, false, false, false, true, true, true, false, false, false, false},
            {true, true, false, false, false, true, false, false, true, true, false, false, true, true, true, true},
            {true, false, false, false, false, true, false, false, false, true, false, false, true, true, false, true},
            {true, true, false, false, false, false, false, false, false, true, false, true, true, true, false, false},
            {true, true, true, true, false, false, false, false, false, true, true, true, true, true, false, false},
            {true, true, true, false, true, true, false, true, true, true, false, false, true, true, true, true},
            {true, true, true, false, true, true, false, true, true, true, false, false, true, true, false, false},
            {true, true, true, false, true, true, false, false, false, true, false, true, true, true, false, false},
            {true, true, true, true, true, true, false, false, false, true, true, true, true, true, false, false},
            {true, false, true, false, true, true, true, true, true, false, true, true, true, true, true, true},
            {true, false, false, false, true, false, true, false, true, false, true, true, true, false, true, true},
            {true, false, true, false, false, false, false, false, true, true, true, true, true, false, true, true},
            {true, true, true, true, false, false, true, false, true, true, true, true, true, false, true, true},
            {true, false, true, false, false, false, true, false, true, false, true, false, true, true, true, true},
            {true, false, true, false, false, false, true, false, false, false, false, false, true, false, true, true},
            {true, false, true, false, false, false, false, false, false, true, true, true, true, false, true, true},
            {true, true, true, true, false, false, false, false, false, true, true, true, true, false, true, false},
            {true, false, false, false, true, false, false, true, true, true, true, false, true, true, true, true},
            {true, false, false, false, true, false, false, false, false, true, false, false, true, true, true, false},
            {true, true, true, true, true, true, true, false, false, true, true, true, true, true, true, false},
            {true, true, true, true, true, true, true, false, false, true, true, true, true, true, true, false},
            {true, true, true, false, true, true, true, true, true, true, true, false, true, true, true, true},
            {true, true, true, false, true, true, false, false, false, true, false, false, true, true, true, false},
            {true, true, true, true, true, true, true, true, false, true, true, false, true, true, false, false},
            {true, true, true, true, true, true, true, true, false, true, true, true, true, true, false, false},
            {true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true},
            {true, false, true, false, true, false, true, false, true, false, true, true, true, false, true, true},
            {true, false, true, true, true, false, true, false, true, true, true, true, true, false, true, true},
            {true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true},
            {true, false, true, false, true, false, true, false, true, false, true, false, true, true, true, true},
            {true, false, true, false, true, false, true, false, false, false, true, false, true, false, true, false},
            {true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false},
            {true, true, true, true, true, false, true, false, false, true, true, true, true, false, true, false},
            {true, false, true, false, true, true, true, true, true, true, true, false, true, true, true, true},
            {true, false, true, false, true, false, true, false, false, true, true, false, true, true, true, false},
            {true, true, true, true, true, true, true, false, false, true, true, true, true, true, true, false},
            {true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false},
            {true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true},
            {true, true, true, false, true, true, true, false, false, true, true, false, true, true, true, false},
            {true, true, true, true, true, true, true, true, false, true, true, false, true, true, true, false},
            {true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false},
        };

        // This is the same deal but for foreground colors
        private readonly static bool[,] s_foregroundsToSkip = new bool[,] {
            {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
            {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
            {false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false},
            {false, false, false, false, false, false, false, false, false, false, false, false, true, false, false, false},
            {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
            {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
            {false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false},
            {true, true, true, false, false, false, false, false, false, true, true, false, false, false, false, false},
            {true, true, true, false, true, true, false, false, false, true, true, false, true, true, false, false},
            {false, false, false, false, true, true, false, false, false, false, false, false, true, true, false, false},
            {false, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false},
            {false, false, true, true, false, false, true, false, true, false, true, true, false, false, false, true},
            {true, true, true, true, true, true, true, true, true, true, true, false, true, true, false, false},
            {true, true, true, false, true, true, true, true, true, true, false, false, true, true, false, false},
            {false, false, false, false, true, true, true, false, true, false, false, false, true, true, false, true},
            {false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, false},
            {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
            {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
            {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
            {true, true, true, false, false, false, false, false, false, false, true, false, false, false, false, false},
            {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
            {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
            {false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false},
            {true, true, true, true, false, false, false, true, false, true, true, false, false, false, false, false},
            {false, true, false, false, false, true, false, false, false, true, false, false, false, true, false, false},
            {false, false, false, false, false, true, false, false, false, false, false, false, false, true, false, false},
            {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
            {true, true, true, true, false, false, false, false, false, false, true, true, false, false, false, false},
            {true, true, true, true, true, true, false, true, true, true, false, false, true, true, false, false},
            {true, true, true, false, true, true, false, true, true, true, false, false, true, true, false, false},
            {true, true, false, false, true, true, false, false, false, false, false, false, true, true, false, false},
            {true, true, true, false, true, false, false, false, false, false, true, false, true, false, false, false},
            {false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
            {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
            {false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false},
            {false, true, false, true, false, true, false, false, true, true, false, true, false, false, false, true},
            {false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false},
            {false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false},
            {false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false},
            {true, true, true, true, false, false, false, false, false, true, true, true, false, false, false, false},
            {false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false},
            {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
            {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
            {false, true, true, true, false, false, false, true, true, true, true, true, false, false, false, false},
            {false, true, false, true, false, true, false, false, true, true, false, false, false, true, false, true},
            {true, true, false, false, true, true, false, false, false, true, false, false, false, true, false, false},
            {false, true, false, false, true, true, false, true, true, true, false, false, true, true, false, false},
            {false, true, false, true, false, true, false, true, true, true, false, false, false, true, false, false},
            {false, false, false, false, false, false, false, false, false, true, false, false, false, false, false, false},
            {true, false, true, false, true, false, false, false, false, false, true, false, false, false, false, false},
            {false, false, false, false, true, true, true, false, true, false, false, false, true, false, true, true},
            {false, false, false, false, false, false, false, false, false, false, false, false, false, true, false, false},
            {true, false, true, false, true, false, false, false, false, false, true, false, true, false, false, false},
            {true, false, true, false, true, false, true, true, false, false, true, false, true, false, false, false},
            {true, false, true, false, true, false, true, false, false, false, true, false, true, false, true, false},
            {true, true, true, false, true, false, false, false, false, true, true, false, true, false, false, false},
            {false, false, true, true, false, false, true, false, true, false, true, false, false, false, true, true},
            {true, false, true, false, true, false, true, false, false, false, true, false, false, false, true, false},
            {false, false, true, false, true, false, true, true, true, false, true, false, true, false, true, false},
            {false, false, true, true, false, false, true, true, true, false, true, true, false, false, true, false},
            {false, false, false, false, false, false, false, false, false, false, false, true, false, false, false, false},
            {true, true, true, false, true, false, false, false, false, true, true, false, false, false, false, false},
            {false, false, false, false, true, true, true, true, true, false, false, false, true, true, false, false},
            {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        };

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
maxCrazy:1.2613896931601563
sumCrazy:6355.646015305313
Crazy Avg:0.06355646015305313
missesCrazy:21841
0|1.2613896931601563|6355.646015305313|0.06355646015305313|21841
1|82.53801590520298|477502.08213154506|4.775020821315451|40094
2|82.53801590520298|477486.88043190155|4.7748688043190155|40078
4|82.53801590520298|477439.49360507354|4.774394936050736|40030
8|82.53801590520298|476334.04026901175|4.763340402690117|39558
16|82.53801590520298|474509.4444054696|4.745094444054696|38748
32|82.53801590520298|472612.96891179896|4.72612968911799|37908
64|82.53801590520298|462692.4972117476|4.626924972117476|35999
128|82.53801590520298|449617.5535501151|4.496175535501151|33702
256|82.53801590520298|432290.39153806353|4.3229039153806355|29785
512|82.53801590520298|397092.8509317036|3.970928509317036|25415
1024|82.53801590520298|383070.54769999906|3.8307054769999906|23592
2048|55.88772264758116|71027.86939605336|0.7102786939605336|8579
4096|55.88772264758116|61276.05130403462|0.6127605130403462|7330
8192|55.88772264758116|61276.05130403462|0.6127605130403462|7330
16384|0|0|0|0
            */
            /*
Count Date form Counting()
Sum
[ 0]       Black    294023    184714
[ 1]    DarkBlue    687509    834300
[ 2]   DarkGreen    454226    496029
[ 3]    DarkCyan   1114063   1293622
[ 4]     DarkRed    687456    834077
[ 5] DarkMagenta    954007   1180261
[ 6]  DarkYellow   1111244   1293547
[ 7]        Gray    606327    471711
[ 8]    DarkGray   1302772    924773
[ 9]        Blue    903639    820570
[10]       Green   1195412    766599
[11]        Cyan   1617875   1886838
[12]         Red    903639    820620
[13]     Magenta   1480123   1523140
[14]      Yellow   1616265   1888586
[15]       White   1848636   1556903
CountsCalls       :   16777216
CountsBackgrounds :   88866816        5
CountsForegrounds :  756116879       45
CountsComputeT    :  602294853       35
CountsComputeTGood:   73016770        4
CountsChangeMatch :   69925105        4
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
                int missesCrazy = 0;

                for (int i = 0; i < StaticColorValidationData.TestColors.Length; i++)
                {
                    var match = ColorUtilities.BestMatch(map, StaticColorValidationData.TestColors[i]).Result;
                    var brute = bestMatches[StaticColorValidationData.TestColors[i]];
                    // Compute the difference in differences.  brute force should yield an items has the mininum difference
                    // But there could be more then one at that distance, and all are equally valid
                    // So compute how much father await we are then that.
                    double dif = Difference(match, StaticColorValidationData.TestColors[i]) - Difference(brute, StaticColorValidationData.TestColors[i]);
                    if (dif != 0.0)
                    {
                        missesCrazy++;
                    }
                    maxCrazy = Math.Max(maxCrazy, dif);
                    sumCrazy += dif;
                }

                Console.WriteLine($"{nameof(maxCrazy)}:{maxCrazy}");
                Console.WriteLine($"{nameof(sumCrazy)}:{sumCrazy}");
                Console.WriteLine($"Crazy Avg:{sumCrazy / (double)colorsToCheck}");
                Console.WriteLine($"{nameof(missesCrazy)}:{missesCrazy}");

                Console.WriteLine($"{0}|{maxCrazy}|{sumCrazy}|{sumCrazy / (double)colorsToCheck}|{missesCrazy}");

                for(int maxChildren = 1; ; maxChildren *= 2)
                {
                    ColorOctree octree = StaticColorValidationData.CreateOctree(maxChildren);
                    var counts = octree.Count();

                    double maxOctree = double.MinValue;
                    double sumOctree = 0;
                    int missesOctree = 0;
                    for (int i = 0; i < StaticColorValidationData.TestColors.Length; i++)
                    {
                        var match = octree.BestMatch(StaticColorValidationData.TestColors[i]).Result;
                        var brute = bestMatches[StaticColorValidationData.TestColors[i]];
                        double dif = Difference(match, StaticColorValidationData.TestColors[i]) - Difference(brute, StaticColorValidationData.TestColors[i]);
                        if (dif != 0.0)
                        {
                            missesOctree++;
                        }
                        maxOctree = Math.Max(maxOctree, dif);
                        sumOctree += dif;
                    }

                    Console.WriteLine($"{maxChildren}|{maxOctree}|{sumOctree}|{sumOctree / (double)colorsToCheck}|{missesOctree}");

                    if (counts.NodesWithLeafs == 1)
                    {
                        break;
                    }
                }
            }

            public static void Counting()
            {
                CharMap map = StaticColorValidationData.Map;
                int colorCount = ColorUtilities.ConsoleColors.Count();

                // +1 b/c we include 0;
                int colorBuckets = 1 + ColorUtilities.MaxColorIndex;

                var background = new int[colorBuckets, colorCount];
                var foreground = new int[colorBuckets, colorCount];

                for(int r = 0; r <= Byte.MaxValue; r++)
                for(int g = 0; g <= Byte.MaxValue; g++)
                for(int b = 0; b <= Byte.MaxValue; b++)
                {
                    Rgb24 color = new Rgb24((byte)r, (byte)g, (byte)b);
                    var match = ColorUtilities.BestMatch(map, color);
                    int index = ColorUtilities.ColorIndex(color);
                    background[index, (int)match.Background]++;

                    // Don't count for forground color for ' ' since any value there would be fine.
                    if (match.Character != ' ')
                    {
                        foreground[index, (int)match.Foreground]++;
                    }
                }

                int[] sumBackground = new int[colorCount];
                int[] sumForeground = new int[colorCount];
                for (int index = 0; index < colorBuckets; index++)
                {
                    Console.Write($"{index,3} {Convert.ToString(index, 2),8} ");
                    int backgroundCountOfZero = 0;
                    int foregroundCountOfZero = 0;
                    foreach(var color in ColorUtilities.ConsoleColors)
                    {
                        int cIndex = (int)color;
                        if (background[index, cIndex] == 0) 
                        {
                            backgroundCountOfZero++;
                        }
                        if (foreground[index, cIndex] == 0)
                        {
                            foregroundCountOfZero++;
                        }
                        sumBackground[cIndex] += background[index, cIndex];
                        sumForeground[cIndex] += foreground[index, cIndex];
                    }
                    Console.WriteLine($"{nameof(backgroundCountOfZero)}:{backgroundCountOfZero,2} {nameof(foregroundCountOfZero)}:{foregroundCountOfZero,2}");
                }
                Console.WriteLine("Sum");
                foreach(var color in ColorUtilities.ConsoleColors)
                {
                    int cIndex = (int)color;
                    Console.WriteLine($"[{cIndex, 2}]{color, 12} {sumBackground[cIndex], 9} {sumForeground[cIndex], 9}");
                }

                Console.WriteLine($"{nameof(ColorUtilities.CountsCalls)}       :{ColorUtilities.CountsCalls, 11}");
                if (ColorUtilities.CountsCalls > 0)
                {
                    Console.WriteLine($"{nameof(ColorUtilities.CountsBackgrounds)} :{ColorUtilities.CountsBackgrounds, 11} {ColorUtilities.CountsBackgrounds/ColorUtilities.CountsCalls, 8}");
                    Console.WriteLine($"{nameof(ColorUtilities.CountsForegrounds)} :{ColorUtilities.CountsForegrounds, 11} {ColorUtilities.CountsForegrounds/ColorUtilities.CountsCalls, 8}");
                    Console.WriteLine($"{nameof(ColorUtilities.CountsComputeT)}    :{ColorUtilities.CountsComputeT, 11} {ColorUtilities.CountsComputeT/ColorUtilities.CountsCalls, 8}");
                    Console.WriteLine($"{nameof(ColorUtilities.CountsComputeTGood)}:{ColorUtilities.CountsComputeTGood, 11} {ColorUtilities.CountsComputeTGood/ColorUtilities.CountsCalls, 8}");
                    Console.WriteLine($"{nameof(ColorUtilities.CountsChangeMatch)} :{ColorUtilities.CountsChangeMatch, 11} {ColorUtilities.CountsChangeMatch/ColorUtilities.CountsCalls, 8}");
                }

                StringBuilder builder = new StringBuilder();
                builder.AppendLine("private readonly static bool[,] s_backgroundsToSkip = new bool[,] {");
                for (int index = 0; index < colorBuckets; index++)
                {
                    builder.Append("  {");
                    List<bool> values = new List<bool>();
                    foreach(var color in ColorUtilities.ConsoleColors)
                    {
                        int cIndex = (int)color;
                        values.Add(background[index, cIndex] == 0);
                    }
                    builder.Append(string.Join(", ", values).ToLower());
                    builder.AppendLine("},");
                }
                builder.AppendLine("};");
                builder.AppendLine("private readonly static bool[,] s_foregroundsToSkip = new bool[,] {");
                for (int index = 0; index < colorBuckets; index++)
                {
                    builder.Append("  {");
                    List<bool> values = new List<bool>();
                    foreach(var color in ColorUtilities.ConsoleColors)
                    {
                        int cIndex = (int)color;
                        values.Add(foreground[index, cIndex] == 0);
                    }
                    builder.Append(string.Join(", ", values).ToLower());
                    builder.AppendLine("},");
                }
                builder.AppendLine("};");
                Console.WriteLine(builder);
            }
        }
    }
}