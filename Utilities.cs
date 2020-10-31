using System;
using System.Runtime.CompilerServices;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Text;
using SixLabors.Primitives;

namespace Ascii3dEngine
{
    public static class Utilities
    {
        // This has show up in a few places, and I wanted to centrails them, so by creating the constant it is easy to track where this is having an effect
        // But basically we measure characters Width to 11, and Hight to be 15.  But if I take a screen shot from my terminal I find that they are 17 pixels wide and 39 pixels high
        // So what we have here is a ratio of (Measured Hight / Measured Width) / (Actual Hight / Actual Width).  If our measurements matched, the fudgeFactor Would be 1
        public const double FudgeFactor = (36.0 / 17.0) / (15.0 / 11.0) ;

        // I should do some more reading here https://docs.microsoft.com/en-us/previous-versions/dotnet/articles/ms973858(v=msdn.10)#highperfmanagedapps_topic10
        // I did some bench marking on aggressinving inlining quite a lot of methods and for matching 100000 colors
        // |        Method |                       Arguments | N |     Mean |   Error |  StdDev |
        // |-------------- |-------------------------------- |-- |---------:|--------:|--------:|
        // | FindAllColors | /p:TESTFLAG=true,/t:Clean;Build | 0 | 328.5 ms | 6.36 ms | 8.71 ms |
        // | FindAllColors |                  /t:Clean;Build | 0 | 347.9 ms | 6.94 ms | 9.95 ms |
        // TESTFLAG was with AggressiveInlining
        // I also looked in to benchmarking MethodImplOptions.AggressiveOptimization, and found that it made little to no difference
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Ratio(int numberator, int denominator) => (int)Math.Ceiling((double)numberator / (double)denominator);
        
        public static void DrawChar(Image<Rgb24> image, char c, int x, int y, Font font, Rectangle sourceRectangle, SolidBrush brush, Pen pen)
        {
            var textProcessor = new DrawTextProcessor(
                s_textOptions,
                new string(c, 1),
                font,
                brush,
                pen,
                new PointF(x, y));

            using (var specificProcessor = textProcessor.CreatePixelSpecificProcessor(image, sourceRectangle))
            {
                specificProcessor.Apply();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (double V1, double V2) Rotate(double v1, double v2, double angle)
        {
            double hypotenuse = Math.Sqrt((v1 * v1) + (v2 * v2));
            double currentAngleInRad = Math.Atan2(v1, v2);
            double newAngleInRad = (angle * Math.PI / (180.0)) + currentAngleInRad;
            return (hypotenuse * Math.Sin(newAngleInRad), hypotenuse * Math.Cos(newAngleInRad));
        }

        // Page 241
        public static double[,] AffineTransformationForRotatingAroundUnit(Point3D unit, double radions, Point3D? start = null)
        {
            double c = Math.Cos(radions);
            double s = Math.Sin(radions);

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

        private readonly static TextGraphicsOptions s_textOptions = new TextGraphicsOptions(enableAntialiasing: false);
    }
}