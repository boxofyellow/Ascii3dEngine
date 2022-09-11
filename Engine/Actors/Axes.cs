using System;
using System.Collections.Generic;

namespace  Ascii3dEngine.Engine
{
    public class Axes : PolygonActorBase
    {
        public Axes(Settings settings) : base(settings, GetData(settings)) { }

        private static (Point3D[] Points, int[][] Faces) GetData(Settings settings)
        {
            double offCenter = settings.AxesScale;
            double length = offCenter * 6;

            var points = new List<Point3D>();
            var faces = new List<int[]>();

            foreach (var point in new [] { Point3D.XUnit * length, Point3D.YUnit * length, Point3D.ZUnit * length})
            {
                int start = points.Count;

                faces.AddRange(new [] {
                    new [] {start + 0, start + 1, start + 2, start + 3},
                    new [] {start + 4, start + 5, start + 6, start + 7},
                });

                points.AddRange(new Point3D[] {
                    new(
                        point.X != 0 ? 0 : -offCenter,
                        point.Y != 0 ? 0 : (point.X != 0 ? -offCenter : offCenter),
                        point.Z != 0 ? 0 : offCenter),
                        
                    new(
                        point.X != 0 ? point.X : -offCenter,
                        point.Y != 0 ? point.Y : (point.X != 0 ? -offCenter : offCenter),
                        point.Z != 0 ? point.Z : offCenter),
                        
                    new(
                        point.X != 0 ? point.X : offCenter,
                        point.Y != 0 ? point.Y : (point.X != 0 ? offCenter : -offCenter),
                        point.Z != 0 ? point.Z : -offCenter),

                    new(
                        point.X != 0 ? 0 : offCenter,
                        point.Y != 0 ? 0 : (point.X != 0 ? offCenter : -offCenter),
                        point.Z != 0 ? 0 : -offCenter),

                    new(
                        point.X != 0 ? 0 : offCenter,
                        point.Y != 0 ? 0 : offCenter,
                        point.Z != 0 ? 0 : offCenter),

                    new(
                        point.X != 0 ? point.X : offCenter,
                        point.Y != 0 ? point.Y : offCenter,
                        point.Z != 0 ? point.Z : offCenter),

                    new(
                        point.X != 0 ? point.X : -offCenter,
                        point.Y != 0 ? point.Y : -offCenter,
                        point.Z != 0 ? point.Z : -offCenter),

                    new(
                        point.X != 0 ? 0 : -offCenter,
                        point.Y != 0 ? 0 : -offCenter,
                        point.Z != 0 ? 0 : -offCenter),
                    });
            }

            return (points.ToArray(), faces.ToArray());
        }

        public override ColorProperties ColorAt(Point3D intersection, int id) => ((id - IdsRangeStart) / 2) switch
        {
            0 => ColorProperties.RedPlastic,
            1 => ColorProperties.YellowPlastic,
            2 => ColorProperties.BluePlastic,
            _ => throw new NotImplementedException("Um how did this happen?")
        };

        // Don't Rotate
        public override void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera) { }

        public override bool DoubleSided(Point3D intersection, int id) => true;
    }
}