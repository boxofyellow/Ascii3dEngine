using System;

namespace Ascii3dEngine
{
    public class InfinitePlane : PolygonActorBase
    {
        public InfinitePlane(Settings settings, ColorProperties properties, double? x = null, double? y = null, double? z = null) 
            : base(settings, GetData(x, y, z)) => m_properties = properties;

        // Don't Rotate
        public override void Act(System.TimeSpan timeDelta, System.TimeSpan elapsedRuntime, Camera camera) { }

        public override ColorProperties ColorAt(Point3D intersection, int id) => m_properties; 

        private static (Point3D[] Points, int[][] Faces) GetData(double? x, double? y, double? z)
        {
            int count = (x == null ? 0 : 1)
                      + (y == null ? 0 : 1)
                      + (z == null ? 0 : 1);

            if (count != 1)
            {
                throw new ArgumentException($"Need to have one, and exactly one plane set, {x}, {y}, {z}");
            }

            // ok so maybe they are not infinite
            double min = -Utilities.MaxRange;
            double max = Utilities.MaxRange;

            Point3D[] points = new [] {
                new Point3D(
                        x ?? min,
                        y ?? min,
                        z ?? min),

                new Point3D(
                        x ?? max,
                        // if x is null, we already have one max
                        y ?? ((x == null) ? min : max),
                        z ?? min),

                new Point3D(
                        x ?? max,
                        y ?? max,
                        z ?? max),

                new Point3D(
                        x ?? min,
                        // if z is null, we already have one max
                        y ?? ((z == null) ? min : max),
                        z ?? max),
            };

            int[][] faces = new [] {new [] {0, 1, 2, 3}};
            return (points, faces);
        }

        private readonly ColorProperties m_properties;
    }
}