using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Ascii3dEngine
{
    public static class Extensions
    {
        public static bool IsBlack<TSelf>(this IPixel<TSelf> pixel) where TSelf : struct, IPixel<TSelf>
        {
            var black = Color.Black.ToPixel<TSelf>(); 
            if (pixel.Equals(black))
            {
                return true;
            }

            var white = Color.White.ToPixel<TSelf>();
            if (pixel.Equals(white))
            {
                return false;
            }

            throw new Exception($"Found pixel ({pixel}) that is not white ({white}) or black ({black})");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point3D Average(this IEnumerable<Point3D> points)
        {
            Point3D result = default;
            int count = 0;
            foreach (var point in points)
            {
                result += point;
                count++;
            }
            return result / count;
        }
    }
}
