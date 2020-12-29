using System;
using System.Collections.Generic;

namespace  Ascii3dEngine
{
    public class Axes : PolygonActorBase
    {
        public Axes(Settings settings, CharMap map) : base(settings, GetData()) => m_map = map;

        private static (Point3D[] Points, int[][] Faces) GetData()
        {
            const double offCenter = 2.5;

            var points = new List<Point3D>();
            var faces = new List<int[]>();

            foreach (var point in new [] { s_x, s_y, s_z})
            {
                int start = points.Count;

                faces.AddRange(new [] {
                    new [] {start + 0, start + 1, start + 2, start + 3},
                    new [] {start + 4, start + 5, start + 6, start + 7},
                });

                points.AddRange(new [] {
                    new Point3D(
                        point.X != 0 ? 0 : -offCenter,
                        point.Y != 0 ? 0 : (point.X != 0 ? -offCenter : offCenter),
                        point.Z != 0 ? 0 : offCenter),
                        
                    new Point3D(
                        point.X != 0 ? point.X : -offCenter,
                        point.Y != 0 ? point.Y : (point.X != 0 ? -offCenter : offCenter),
                        point.Z != 0 ? point.Z : offCenter),
                        
                    new Point3D(
                        point.X != 0 ? point.X : offCenter,
                        point.Y != 0 ? point.Y : (point.X != 0 ? offCenter : -offCenter),
                        point.Z != 0 ? point.Z : -offCenter),

                    new Point3D(
                        point.X != 0 ? 0 : offCenter,
                        point.Y != 0 ? 0 : (point.X != 0 ? offCenter : -offCenter),
                        point.Z != 0 ? 0 : -offCenter),

                    new Point3D(
                        point.X != 0 ? 0 : offCenter,
                        point.Y != 0 ? 0 : offCenter,
                        point.Z != 0 ? 0 : offCenter),

                    new Point3D(
                        point.X != 0 ? point.X : offCenter,
                        point.Y != 0 ? point.Y : offCenter,
                        point.Z != 0 ? point.Z : offCenter),

                    new Point3D(
                        point.X != 0 ? point.X : -offCenter,
                        point.Y != 0 ? point.Y : -offCenter,
                        point.Z != 0 ? point.Z : -offCenter),

                    new Point3D(
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
        public override void Act(System.TimeSpan timeDelta, System.TimeSpan elapsedRuntime, Camera camera) { }

        public override bool DoubleSided(Point3D intersection, int id) => true;

        public override void Render(Projection projection, bool[,] imageData, List<Label> labels)
        {
            Point3D origin = new Point3D();
            imageData.DrawLine(projection, origin, s_x);
            imageData.DrawLine(projection, origin, s_y);
            imageData.DrawLine(projection, origin, s_z);

            (bool inView, _, Point2D p2) = projection.Trans_Line(origin, s_lX);
            if (inView)
            {
                labels.Add(new Label(
                    p2.H / m_map.MaxX,
                    p2.V / m_map.MaxY,
                    'X'));
            }

            (inView, _, p2) = projection.Trans_Line(origin, s_lY);
            if (inView)
            {
                labels.Add(new Label(
                    p2.H / m_map.MaxX,
                    p2.V / m_map.MaxY,
                    'Y'));
            }

            (inView, _, p2) = projection.Trans_Line(origin, s_lZ);
            if (inView)
            {
                labels.Add(new Label(
                    p2.H / m_map.MaxX,
                    p2.V / m_map.MaxY,
                    'Z'));
            }
        }

        private readonly CharMap m_map;

        private static Point3D s_x = new Point3D(15, 0 , 0 );
        private static Point3D s_y = new Point3D(0 , 15, 0 );
        private static Point3D s_z = new Point3D(0 , 0 , 15);

        private static Point3D s_lX = s_x * 1.25;
        private static Point3D s_lY = s_y * 1.25;
        private static Point3D s_lZ = s_z * 1.25;
    }
}