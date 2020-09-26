using System.Collections.Generic;

namespace Ascii3dEngine
{
    public class Cube : Actor
    {
        public Cube(Settings settings, CharMap map, Point3D origin = default) : base(origin)
        {
            m_spin = settings.Spin;
            m_hideBack = settings.HideBack;
            m_map = map;

            for (int i = default; i < m_points.Length; i++)
            {
                m_points[i] = m_points[i] * (c_size / 2.0);
            }
        }

        public override void Act(System.TimeSpan timeDelta, System.TimeSpan elapsedRuntime, Camera camera)
        {
            if (m_spin)
            {
                double delta = timeDelta.TotalSeconds * 15.0;
                delta %= 360.0;
                if (delta < 0) delta += 360.0;
                Point3D deltaAngle = new Point3D(delta, -delta, delta / 2.0);
                for (int i = default; i < m_points.Length; i++)
                {
                    m_points[i] = m_points[i].Rotate(deltaAngle);
                }
            }
        }

        public override void Render(Projection projection, bool[,] imageData, List<Label> lables)
        {
            AllLines.Clear();

            AddFace('F', // front
                m_points[c_frontUpperRight],
                m_points[c_frontUpperLeft],
                m_points[c_frontLowerLeft],
                m_points[c_frontLowerRight]
            );

            AddFace('B', // back
                m_points[c_backUpperLeft],
                m_points[c_backUpperRight],
                m_points[c_backLowerRight],
                m_points[c_backLowerLeft]
            );

            AddFace('R', // right
                m_points[c_backUpperRight],
                m_points[c_frontUpperRight],
                m_points[c_frontLowerRight],
                m_points[c_backLowerRight]
            );

            AddFace('L', // left
                m_points[c_frontUpperLeft],
                m_points[c_backUpperLeft],
                m_points[c_backLowerLeft],
                m_points[c_frontLowerLeft]
            );

            AddFace('T', // upper
                m_points[c_frontUpperLeft],
                m_points[c_frontUpperRight],
                m_points[c_backUpperRight],
                m_points[c_backUpperLeft]
            );

            AddFace('T', // Lower
                m_points[c_frontLowerRight],
                m_points[c_frontLowerLeft],
                m_points[c_backLowerLeft],
                m_points[c_backLowerRight]
            );

            base.Render(projection, imageData, lables);

            void AddFace(char l, Point3D p1, Point3D p2, Point3D p3, Point3D p4)
            {
                Point3D average = new Point3D(
                    (p1.X + p2.X + p3.X + p4.X)/4.0,
                    (p1.Y + p2.Y + p3.Y + p4.Y)/4.0,
                    (p1.Z + p2.Z + p3.Z + p4.Z)/4.0);

                if (m_hideBack)
                {
                    Point3D normal = (p1 - average).CrossProduct(p2 - average);
                    normal.Normalize();
                    // when the dot product is > 0 it is a "back plane" (pointing away from the camera)
                    if ((Origin + average - projection.Camera.From).DotProduct(normal) > 0.0)
                    {
                        return;
                    }
                }

                (bool inView, _, Point2D projectedP2) = projection.Trans_Line(new Point3D(), new Point3D(average));
                if (inView)
                {
                    lables.Add(new Label(
                        projectedP2.H / m_map.MaxX,
                        projectedP2.V / m_map.MaxY,
                        l));
                }

                AllLines.AddRange(new[] {
                    new Line3D(p1, p2),
                    new Line3D(p2, p3),
                    new Line3D(p3, p4),
                    new Line3D(p4, p1),
                });
            }
        }

        private readonly Point3D m_angle = new Point3D();

        private readonly Point3D[] m_points = new []
        {
            new Point3D(-1, 1, 1),   // font upper left
            new Point3D(1, 1, 1),    // font upper right
            new Point3D(-1, -1, 1),  // font lower left
            new Point3D(1, -1, 1),   // font lower right

            new Point3D(-1, 1, -1),  // back upper left
            new Point3D(1, 1, -1),   // back upper right
            new Point3D(-1, -1, -1), // back lower left
            new Point3D(1, -1, -1),  // back lower right
        };

        private const int c_frontUpperLeft =  0;
        private const int c_frontUpperRight =  1;
        private const int c_frontLowerLeft =  2;
        private const int c_frontLowerRight =  3;
        private const int c_backUpperLeft =  4;
        private const int c_backUpperRight =  5;
        private const int c_backLowerLeft =  6;
        private const int c_backLowerRight =  7;

        private readonly bool m_spin;
        private readonly bool m_hideBack;
        private readonly CharMap m_map;

        private const double c_size = 25;
    }
}