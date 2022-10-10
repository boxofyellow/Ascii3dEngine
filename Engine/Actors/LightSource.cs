using SixLabors.ImageSharp.PixelFormats;

public class LightSource
{
    public LightSource(Point3D point, Rgb24 color)
    {
        Point = point;
        Color = color;
    }

    public virtual void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera) { }

    public readonly Rgb24 Color;
    public Point3D Point { get; set; }
}