using System.Numerics;
using System.Runtime.CompilerServices;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing.Processing.Processors.Text;
using SixLabors.ImageSharp.PixelFormats;

public static class Utilities
{
    // Just something big, but "unlikely" to overflow
    static Utilities() => MaxRange = Math.Sqrt(Math.Sqrt(double.MaxValue)) / 1000.0;

    // This has show up in a few places, and I wanted to centralize them, so by creating the constant it is easy to track where this is having an effect
    // But basically we measure characters Width to 11, and Hight to be 15.  But if I take a screen shot from my terminal I find that they are 17 pixels wide and 39 pixels high
    // So what we have here is a ratio of (Measured Hight / Measured Width) / (Actual Hight / Actual Width).  If our measurements matched, the fudgeFactor Would be 1
    public const double FudgeFactor = (36.0 / 17.0) / (15.0 / 11.0) ;

    // I should do some more reading here https://docs.microsoft.com/en-us/previous-versions/dotnet/articles/ms973858(v=msdn.10)#highperfmanagedapps_topic10
    // I did some benchmarking on aggressively inlining quite a lot of methods and for matching 100000 colors
    // |        Method |                       Arguments | N |     Mean |   Error |  StdDev |
    // |-------------- |-------------------------------- |-- |---------:|--------:|--------:|
    // | FindAllColors | /p:TESTFLAG=true,/t:Clean;Build | 0 | 328.5 ms | 6.36 ms | 8.71 ms |
    // | FindAllColors |                  /t:Clean;Build | 0 | 347.9 ms | 6.94 ms | 9.95 ms |
    // TESTFLAG was with AggressiveInlining
    // I also looked in to benchmarking MethodImplOptions.AggressiveOptimization, and found that it made little to no difference
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Ratio(int numerator, int denominator) => (int)Math.Ceiling((double)numerator / (double)denominator);
    
    public static void DrawChar(Image<Rgb24> image, char c, int x, int y, Font font, Rectangle sourceRectangle, SolidBrush brush, Pen pen)
    {
        var textProcessor = new DrawTextProcessor(
            s_drawingOptions,
            new TextOptions(font) { Origin = new Vector2(x, y)},
            new string(c, 1),
            brush,
            pen
        );

        using var specificProcessor = textProcessor.CreatePixelSpecificProcessor(Configuration.Default, image, sourceRectangle);
        specificProcessor.Execute();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double DegreesToRadians(double degrees) => degrees * c_radiansPerDegree;

    // Page 241
    public static double[,] AffineTransformationForRotatingAroundUnit(Point3D unit, double radians, Point3D? start = null)
    {
        double c = Math.Cos(radians);
        double s = Math.Sin(radians);

        double omc = 1.0 - c;

        double uXY = unit.X * unit.Y;
        double uXZ = unit.X * unit.Y;
        double uYZ = unit.Y * unit.Z;

        double uXX = unit.X * unit.X;
        double uYY = unit.Y * unit.Y;
        double uZZ = unit.Z * unit.Z;

        return new [,]
        {
            {
                c + (omc * uXX),
                omc * uXY - (s * unit.Z),
                (omc * uXZ) + (s * unit.Y),
                default,
            },
            {
                (omc * uXY) + (s * unit.Z),
                c + (omc * uYY),
                (omc * uYZ) - (s * unit.X),
                default, 
            },
            {
                (omc * uXZ) - (s * unit.Y),
                (omc * uYZ) + (s * unit.X),
                c + (omc * uZZ),
                default,
            },
            {
                start?.X ?? default,
                start?.Y ?? default,
                start?.Z ?? default,
                1.0,
            },
        };
    }

    public readonly static double MaxRange;

    private readonly static DrawingOptions s_drawingOptions = new() { GraphicsOptions = new() { Antialias = false }};

    // 360° = 2𝜋 
    private const double c_radiansPerDegree = Math.PI / 180.0;
}