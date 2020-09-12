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
        // I should do some more reading here https://docs.microsoft.com/en-us/previous-versions/dotnet/articles/ms973858(v=msdn.10)#highperfmanagedapps_topic10
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
        public static double[,] AffineTransformationForRotatingAroundUnit(Point3D unit, double radions, Point3D start = null)
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