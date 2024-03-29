using System.Runtime.CompilerServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

public static class Extensions
{
    public static bool IsBlack<TSelf>(this IPixel<TSelf> pixel) where TSelf : unmanaged, IPixel<TSelf>
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
        int count = default;
        foreach (var point in points)
        {
            result += point;
            count++;
        }
        return result / count;
    }

    public static TSelf[,] GetPixelData<TSelf>(this Image<TSelf> image) where TSelf : unmanaged, IPixel<TSelf>
    {
        TSelf[,] result = new TSelf[image.Height, image.Width];
        int y = default;
        int x = default;

        foreach (var group in image.GetPixelMemoryGroup())
        foreach (var pixel in group.Span)
        {
            result[y, x] = pixel;
            x++;
            if (x == image.Width)
            {
                x = default;
                y++;
            }
        }
        return result;
    }
}
