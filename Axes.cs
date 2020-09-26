using System.Collections.Generic;

namespace  Ascii3dEngine
{
    public class Axes : Actor
    {
        public Axes(CharMap map) : base()
        {
            m_map = map;
            Point3D origin = new Point3D();

            AllLines.AddRange(new [] {
                new Line3D(origin, s_x),
                new Line3D(origin, s_y),
                new Line3D(origin, s_z),
            });
        }

        public override void Render(Projection projection, bool[,] imageData, List<Label> lables)
        {
            base.Render(projection, imageData, lables);

            (bool inView, _, Point2D p2) = projection.Trans_Line(new Point3D(), new Point3D(s_lX));
            if (inView)
            {
                lables.Add(new Label(
                    p2.H / m_map.MaxX,
                    p2.V / m_map.MaxY,
                    'X'));
            }

            (inView, _, p2) = projection.Trans_Line(new Point3D(), new Point3D(s_lY));
            if (inView)
            {
                lables.Add(new Label(
                    p2.H / m_map.MaxX,
                    p2.V / m_map.MaxY,
                    'Y'));
            }

            (inView, _, p2) = projection.Trans_Line(new Point3D(), new Point3D(s_lZ));
            if (inView)
            {
                lables.Add(new Label(
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