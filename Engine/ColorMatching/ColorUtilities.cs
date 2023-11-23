using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public static class ColorUtilities
{
    static ColorUtilities()
    {
        s_allConsoleColors.AddRange(
            Enum.GetValues(typeof(ConsoleColor))
                .OfType<ConsoleColor>()
                .OrderBy(x => x));

        NumberOfConsoleColors = s_allConsoleColors.Count;
    }

    public static IEnumerable<ConsoleColor> ConsoleColors => s_allConsoleColors;

    // These are only set when PROFILECOLOR is true
    public static int CountsCalls = 0;
    public static int CountsTrueLoop = 0;
    public static int CountsBackgrounds = 0;
    public static int CountsForegrounds = 0;
    public static int CountsComputeT = 0;
    public static int CountsComputeTGood = 0;
    public static int CountsChangeMatch = 0;

    public static (char Character, ConsoleColor Foreground, ConsoleColor Background, Rgb24 Result) BestMatch(CharMap map, Rgb24 target)
    {
#if (PROFILECOLOR)
        CountsCalls++;
#endif

        // So this looks rather complicated, did it save us anything?
        // See BruteForce and ColorMatchingBenchmarks

        int tR = target.R;
        int tG = target.G;
        int tB = target.B;

#if (!GENERATECOUNTS)
        int colorIndex = ColorIndex(target);
#endif

        // Instead of computing and comparing true distance, we can compute just the proxy, and avoid using Math.Sqrt, this is true even for a Crazy geometry approach
        // The benchmark for this change does not show as large of an improvement as I was expecting for searching for 100000 random colors
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
        Span<int> pointDistances = stackalloc int[NumberOfConsoleColors];
        Span<bool> pointReady = stackalloc bool[NumberOfConsoleColors];

        for (int i = 0; i < pointDistances.Length; i++)
        {
#if (!GENERATECOUNTS)
            // If we are ignoring this color, we don't even need to bother computing the distance or marking it ready to use
            if (map.BackgroundsToSkip[colorIndex, i] && map.ForegroundsToSkip[colorIndex, i])
            {
                pointDistances[i] = int.MaxValue;
                pointReady[i] = false;
                continue;
            }
#endif
            pointDistances[i] = DifferenceProxy(map.NamedColor(i), target);
            pointReady[i] = true;
        }

        // track this so that if fail find one, we having something to fall back on
        int closestIndex = -1;

        // This loop will happen NumberOfConsoleColors (16) times
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
            // But not only does this allow us narrow resultDistance as fast as possible and inturn check less stuff, it also means we throw points out once we test them
            // so instead of testing n^2 (16*16=256) lines, we really test C(n,k) or C(16,2) 16!/(n!(n-k!)) 20922789888000/(2 * 87178291200) = 120
            // And that is where the big savings come in.
            //
            // One thing that I was not expecting, is that this change appear to have affected the accuracy
            //                        |             max |              sum |              avg
            // Optimization in place 0|83.31866537577280|109479.43344999844|1.0947943344999844
            //               removed 0|85.98837130682264|109670.83507204286|1.0967083507204285
            // So I'm not sure how removing the optimization made it more inaccurate 🤷🏽‍♂️
            //
            // I looked into changing using a sort... but these are really short and it does not look like that will help
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

            if (closestIndex < 0)
            {
                closestIndex = selectedIndex;
            }

            // mark this one used, this is what keeps us to at most 16 loops
            pointReady[selectedIndex] = false;

#if (!GENERATECOUNTS)
            if (map.BackgroundsToSkip[colorIndex, selectedIndex])
            {
                continue;
            }
#endif

#if (PROFILECOLOR)
            CountsBackgrounds++;
#endif

            // this will be our candidate background color
            var selected = map.NamedColor(selectedIndex);
            double p1R = selected.R;
            double p1G = selected.G;
            double p1B = selected.B;

            // I don't think there is any benefits to try to pick the closest (or even farthest) second color
            // This loop does "try" all the colors, but we skip those already processed
            for (int secondIndex = 0; secondIndex < pointReady.Length; secondIndex++)
            {
                if (pointReady[secondIndex])
                {

#if (!GENERATECOUNTS)
                    if (map.ForegroundsToSkip[colorIndex, secondIndex])
                    {
                        continue;
                    }
#endif

#if (PROFILECOLOR)
                    CountsForegrounds++;
#endif

                    // this will be our candidate foreground color
                    var second = map.NamedColor(secondIndex);
                    double p2R = second.R;
                    double p2G = second.G;
                    double p2B = second.B;

                    double vR = p2R - p1R;
                    double vG = p2G - p1G;
                    double vB = p2B - p1B;

                    // We now have two "points" now we need to compute the distance between this line and the target
                    // https://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line
                    // and
                    // https://www.youtube.com/watch?v=g2h3H0FkLjA
                    // Plus all that stuff we cached
                    // Remember
                    //  (stuff WITH target)          = vR * target.R + vG * target.G + vB * target.B
                    double numerator = (vR * tR) + (vG * tG) + (vB * tB) + map.CachedStaticNumeratorDenominators(selectedIndex, secondIndex);

                    // we are about to compute t, before we do that there is some filtering that we can do at this point
                    // if t = 0, the background color is the target
                    // if t = 1, the foreground color is the target
                    // if t < 0, that means the targe color is on the wrong side of the background color, there is no amount of foreground color we could replace to get to the target
                    // the value we cached for the denominator will always be positive (sum of natural number squares)
                    // so if the numerator is negative, then t will be as well
                    if (numerator < 0)
                    {
                        // This one may not be out of the running all together
                        // If this is the closest based color we may still use it we boot everything else out
                        continue;
                    }

#if (PROFILECOLOR)
                    CountsComputeT++;
#endif

                    double t = numerator / map.CachedDenominators(selectedIndex, secondIndex);

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
                        {nameof(map.CachedStaticNumeratorDenominators)}:{map.CachedStaticNumeratorDenominators(selectedIndex, secondIndex)}
                        {nameof(map.CachedDenominators)}:{map.CachedDenominators(selectedIndex, secondIndex)}
                        {nameof(vR)}:{vR}
                        {nameof(vG)}:{vG}
                        {nameof(vB)}:{vB}");
                    }

                    //
                    // The Rounding here should have very little impact on the result b/c we don't use this in the final calculation but we do use it to eliminate things
                    // But it can have an effect.  An Example
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

                    // There is a rounding that is happening here, and really its not even rounding, but truncation instead.  But this is unlikely to make a big differences
                    // the only time the routing here will make a difference is when the line we are checking is close to the result distance AND the point we will choose on that line
                    // is right at that intersection point, but we can nullify that truncation error by simply checking lines that are at least as close, they don't have be strictly closer
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
                        // Showed none of character have more black pixels than white ones (aks the filled in blocks █, ascii 9608) are not included
                        // This means our options would go count = 0 => all background/no foreground, then as count increases we would get more and more foreground.
                        // Then at t = 0.5 we would flip (there would be more foreground pixels than background).
                        // We know that Background is closer to target, so r(t) needs to be closer to Background than Foreground (basically that t is guaranteed to <= 0.5).
                        // The up-shot of this, is that we still don't need ImageProcessing's ability to also check "inverses"

                        (char c, double pixelRatio) = map.PickFromRatio(t);

                        // we can compute this using our r(t) equation
                        Rgb24 currentColor = new(
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

        if (character == default)
        {
            // We failed to find one...
            // This will happen if target is "out side" all of our colors
            // When that happens our `if (numerator < 0)` check filters everything out
            // In this case our best options will be the our closet single color
            character = ' ';
            background = (ConsoleColor)closestIndex;
            foreground = background; // we are using ' ', so this won't matter 
            result = map.NamedColor(closestIndex);
        }

        return (character, foreground, background, result);
    }

    // You might think that you need Min/Max checks here, but those are not necessary and here is why
    //   They would only have an effect when ColorValue we are choosing is "close" those edges.
    //   If p is small (or even 0) then v will be positive (because v will point in the direction of Foreground, and we already decided that p, the Background is small)
    //   The same holds true if p is large, then v will negative (since it points from our larger Background to smaller Foreground)
    //   "t<0.5"
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ColorValue(byte p, double pixelRatio, double v) 
        => (byte)(p + (byte)Math.Round(pixelRatio * v));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Difference(Rgb24 c1, Rgb24 c2) => Math.Sqrt(DifferenceProxy(c1, c2));

    /// <summary>
    /// This value is a "proxy" for difference between these two colors, it is cheaper to compute
    /// But retains the property if the DifferenceProxy(c1, c2) < DifferenceProxy(c1, c3) then Difference(c1, c2) < Difference(c1, c3), same goes for ==
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DifferenceProxy(Rgb24 c1, Rgb24 c2)
    {
        int dR = (int)c1.R - (int)c2.R;
        int dG = (int)c1.G - (int)c2.G;
        int dB = (int)c1.B - (int)c2.B;
        return (dR * dR) + (dG * dG) + (dB * dB);
    }

    // This method is used to partition our color space, in its current from it is more or less an even distribution
    // But I there may be other distributions that may work, we do need to keep computing it fast.  But the most important
    // Attribute is this partitions the colors so the hight number of background (or potential foreground) colors can be ignored.
    // 
    // While testing this out I enumerated all colors and collected profiling data to see how far we got for each attempt
    // Here is the data without optimization
    // CountsCalls       :   16777216
    // CountsBackgrounds :  268435456       16
    // CountsForegrounds : 2013265920      120
    // CountsComputeT    : 1518060147       90
    // CountsComputeTGood:   80687610        4
    // CountsChangeMatch :   77321761        4
    //
    // And here are the results after breaking Red, Green, Blue down the middle
    // CountsCalls       :   16777216
    // CountsBackgrounds :  182368897       10
    // CountsForegrounds : 1729218160      103
    // CountsComputeT    : 1284964529       76
    // CountsComputeTGood:   80670346        4
    // CountsChangeMatch :   77319143        4
    //
    // The Call count remains the same b/c both test all colors, we reduce the Background colors to ~67%
    // And foreground and times that we compute T to ~85%
    // And from benchmarking it
    // |        Method |                             Arguments | N |     Mean |   Error |  StdDev |
    // |-------------- |-------------------------------------- |-- |---------:|--------:|--------:|
    // | FindAllColors | /p:GENERATECOUNTS=true,/t:Clean;Build | 0 | 346.9 ms | 6.21 ms | 8.29 ms |
    // | FindAllColors |                        /t:Clean;Build | 0 | 294.1 ms | 3.73 ms | 2.91 ms |
    // GENERATECOUNTS=true means the optimization is not in place.
    // So with it there it reduces the run time by of testing 100000 colors to about ~85%
    //
    // Changing this to divide each color dimension into 4 sections instead of 2 yields even more savings
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
    // TESTFLAG=true means spiting by 4, without it means splitting by 2
    // There it reduced to about ~71%.  But this change is not FREE... s_backgroundsToSkip grows.
    // Before it (2 * 2 * 2) or 8 x 16 bools
    // Now it is (4 * 4 * 4) or 64 x 16 bools
    // But that array is static 
    // Comparing across is not really valid by since 294.1 and 297.9 so close you could say this about a 40% savings overall
    //
    // From reviewing the counts it looks like this new split also has a bunch of zero for the foregrounds, so we should be able
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
    // This shows we reduce the run time of checking 100000 colors to 80% and just to compare this without any of this optimization
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

    public static int MaxColorIndex => ColorIndex(new Rgb24(byte.MaxValue, byte.MaxValue, byte.MaxValue));

    public static Rgb24[] ComputePureNamedColors()
    {
        var namedColors = typeof(Color)
            .GetFields(BindingFlags.Public | BindingFlags.Static)                 // Get all the public Static Fields
            .Where(f => f.FieldType == typeof(Color) && f.IsInitOnly)             // We only want the Readonly Color ones
            .ToDictionary(f => f.Name,                                            // Map their Name to the value in a Dictionary that ignores case
                            f => ((Color)f.GetValue(default)!).ToPixel<Rgb24>(),  // We want the RGB values
                            StringComparer.OrdinalIgnoreCase);

        if (!namedColors.ContainsKey(ConsoleColor.DarkYellow.ToString()))
        {
            // it looks like they have don't have Dark Yellow, so just throw Dark Goldenrod in there...
            // Without this we find like 10147, with a max difference of 8, with it we find 11771 (an addition of like 16%) and max difference of 7 (and a reduction of like 13%)
            // But from looking at the ColorChat and look from 50,50,50 to the origin, there are two distinct yellow lines
            // The others (green, cyan, blue, magenta and red) have a "Dark version" that overlaps so we need a little work picking a better match
            // With this change it brings the number of unique colors to 11576
            var yellow = namedColors[ConsoleColor.Yellow.ToString()];
            double ration = (ComputeColorRation(namedColors, ConsoleColor.Magenta) + ComputeColorRation(namedColors, ConsoleColor.Cyan)) / 2.0;
            var darkYellow = new Rgb24(
                (byte)((double)(yellow.R) * ration),
                (byte)((double)(yellow.G) * ration),
                (byte)((double)(yellow.B) * ration));
            namedColors.Add(ConsoleColor.DarkYellow.ToString(), darkYellow);
        }

        return ConsoleColors.Select(x => namedColors[x.ToString()]).ToArray();
    }

    private static double ComputeColorRation(Dictionary<string, Rgb24> namedColors, ConsoleColor color)
    {
        var baseColor = namedColors[color.ToString()];
        var darkColor = namedColors[$"Dark{color}"];

        int sumOfBase = (int)baseColor.R + (int)baseColor.G + (int)baseColor.B;
        int sumOfDark = (int)darkColor.R + (int)darkColor.G + (int)darkColor.B;

        return ((double)sumOfDark) / ((double)sumOfBase);
    }

    public static readonly int NumberOfConsoleColors;

    private readonly static List<ConsoleColor> s_allConsoleColors = new();

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
        // To be clear the two methods might return difference values that are equivalent, but also differences resulting in additional variances of the target
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
Count Data form Counting()
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
            var map = StaticColorValidationData.Map;

            (var bestMatches, double maxError, double sumError) = StaticColorValidationData.BestMatches;
            int colorsToCheck = StaticColorValidationData.TestColors.Length;

            Console.WriteLine($"{nameof(maxError)}:{maxError}");
            Console.WriteLine($"{nameof(sumError)}:{sumError}");
            Console.WriteLine($"Avg Error: {sumError / (double)colorsToCheck}");
            Console.WriteLine($"Unique Test cases: {bestMatches.Count} of {colorsToCheck} = ({bestMatches.Count * 100 / colorsToCheck}%)");

            double maxCrazy = double.MinValue;
            double sumCrazy = 0;
            int missesCrazy = 0;

            for (int i = 0; i < StaticColorValidationData.TestColors.Length; i++)
            {
                var match = BestMatch(map, StaticColorValidationData.TestColors[i]).Result;
                var brute = bestMatches[StaticColorValidationData.TestColors[i]];
                // Compute the difference in differences.  brute force should yield an items has the minimum difference
                // But there could be more than one at that distance, and all are equally valid
                // So compute how much father away we are than that.
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
                var octree = StaticColorValidationData.CreateOctree(maxChildren);
                (_, _, var nodesWithLeafs) = octree.Count();

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

                if (nodesWithLeafs == 1)
                {
                    break;
                }
            }
        }

        public static (string[] BackgroundsToSkip, string[] ForegroundsToSkip) ComputeStaticSkip(CharMap map)
        {

#if (!GENERATECOUNTS)
            if ("This if is here so that the compiler thinks rest of this will run".Length > 0)
            {
                throw new ApplicationException($"{nameof(ComputeStaticSkip)} should only b/c called when GENERATECOUNTS is set");
            }
#endif
            int colorCount = ConsoleColors.Count();

            // +1 b/c we include 0;
            int colorBuckets = 1 + MaxColorIndex;

            var background = new int[colorBuckets, colorCount];
            var foreground = new int[colorBuckets, colorCount];

            for(int r = 0; r <= byte.MaxValue; r++)
            for(int g = 0; g <= byte.MaxValue; g++)
            for(int b = 0; b <= byte.MaxValue; b++)
            {
                var color = new Rgb24((byte)r, (byte)g, (byte)b);
                (var matchCharacter, var matchForeground, var matchBackground, var _) = BestMatch(map, color);
                int index = ColorIndex(color);
                background[index, (int)matchBackground]++;

                // Don't count for foreground color for ' ' since any value there would be fine.
                if (matchCharacter != ' ')
                {
                    foreground[index, (int)matchForeground]++;
                }
            }

            int[] sumBackground = new int[colorCount];
            int[] sumForeground = new int[colorCount];
            for (int index = 0; index < colorBuckets; index++)
            {
                Console.Write($"{index,3} {Convert.ToString(index, 2),8} ");
                int backgroundCountOfZero = 0;
                int foregroundCountOfZero = 0;
                foreach(var color in ConsoleColors)
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
            foreach(var color in ConsoleColors)
            {
                int cIndex = (int)color;
                Console.WriteLine($"[{cIndex, 2}]{color, 12} {sumBackground[cIndex], 9} {sumForeground[cIndex], 9}");
            }

            Console.WriteLine($"{nameof(CountsCalls)}       :{CountsCalls, 11}");
            if (CountsCalls > 0)
            {
                Console.WriteLine($"{nameof(CountsBackgrounds)} :{CountsBackgrounds, 11} {CountsBackgrounds/CountsCalls, 8}");
                Console.WriteLine($"{nameof(CountsForegrounds)} :{CountsForegrounds, 11} {CountsForegrounds/CountsCalls, 8}");
                Console.WriteLine($"{nameof(CountsComputeT)}    :{CountsComputeT, 11} {CountsComputeT/CountsCalls, 8}");
                Console.WriteLine($"{nameof(CountsComputeTGood)}:{CountsComputeTGood, 11} {CountsComputeTGood/CountsCalls, 8}");
                Console.WriteLine($"{nameof(CountsChangeMatch)} :{CountsChangeMatch, 11} {CountsChangeMatch/CountsCalls, 8}");
            }

            var backgroundsToSkip = new bool[colorBuckets, colorCount];
            var foregroundsToSkip = new bool[colorBuckets, colorCount];

            for (int index = 0; index < colorBuckets; index++)
            {
                foreach(var color in ConsoleColors)
                {
                    int cIndex = (int)color;
                    backgroundsToSkip[index, cIndex] = background[index, cIndex] == 0;
                    foregroundsToSkip[index, cIndex] = foreground[index, cIndex] == 0;
                }
            }

            return (SerializerStaticSkips(backgroundsToSkip), SerializerStaticSkips(foregroundsToSkip));
        }

        public static bool[,] DeserializerStaticSkips(string[] data)
        {
            int colorBuckets = 1 + MaxColorIndex;

            if (colorBuckets != data.Length)
            {
                throw new ApplicationException($"Expected {colorBuckets} rows, but found {data.Length}");
            }

            int colorCount = ConsoleColors.Count();

            var result = new bool[colorBuckets, colorCount];

            for (int bucket = 0; bucket < result.GetLength(0); bucket++)
            {
                string row = data[bucket];
                if (row.Length != colorCount)
                {
                    throw new ApplicationException($"Expected {colorCount} rows, but found {row.Length}, in row {bucket}");
                }

                for (int colorIndex = 0; colorIndex < result.GetLength(1); colorIndex++)
                {
                    result[bucket, colorIndex] = row[colorIndex] == '1';
                }
            }

            return result;
        }

        public static string[] SerializerStaticSkips(bool[,] data)
        {
            var result = new string[data.GetLength(0)];
            var builder = new StringBuilder();
            for (int bucket = 0; bucket < data.GetLength(0); bucket++)
            {
                for (int colorIndex = 0; colorIndex < data.GetLength(1); colorIndex++)
                {
                    builder.Append(data[bucket, colorIndex] ? '1' : '0');
                }
                result[bucket] = builder.ToString();
                builder.Clear();
            }
            return result;
        }
    }
}