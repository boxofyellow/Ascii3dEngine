using System;
using SixLabors.ImageSharp.PixelFormats;

namespace Ascii3dEngine
{
    public class LightSource
    {
        public LightSource(Point3D point, Rgb24 color)
        {
            Point = point;
            Color = color;
        }

        public virtual void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera) { }

        public Rgb24 Color {get; private set;}
        public Point3D Point {get; private set;}
    }
}