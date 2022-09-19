using System;

namespace Ascii3dEngine.Engine
{
    public class InfinitePlane : PolygonActorBase
    {
        public InfinitePlane(ColorProperties properties, double? x = null, double? y = null, double? z = null) 
            : base(GetData(x, y, z)) => m_properties = properties;

        // Don't Move
        public override void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera) { }

        public override ColorProperties ColorAt(Point3D intersection, int id) => m_properties; 

        public override bool DoubleSided(Point3D intersection, int id) => true;

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

            Point3D[] points = new Point3D[] {
                new(
                    x ?? min,
                    y ?? min,
                    z ?? min),

                new(
                    x ?? max,
                    // if x is null, we already have one max
                    y ?? ((x == null) ? min : max),
                    z ?? min),

                new(
                    x ?? max,
                    y ?? max,
                    z ?? max),

                new(
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