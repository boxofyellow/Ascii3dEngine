using System.Collections.Generic;

namespace  Ascii3dEngine
{
    public class Axes : Actor
    {
        public Axes(CharMap map) : base() => m_map = map;

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