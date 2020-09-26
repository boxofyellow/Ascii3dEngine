using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascii3dEngine
{
    public class Model : Actor
    {
        public Model(Settings settings, Point3D origin = default) : base(origin)
        {
            m_spin = settings.Spin;
            m_hideBack = settings.HideBack;
            (m_points, m_faces) = WaveObjFormParser.Parse(settings.ModelFile);
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

            for (int i = default; i < m_faces.Length; i++)
            {
                Point3D[] points = m_faces[i]
                    .Select(x => m_points[x])
                    .ToArray();

                if (points.Length < 2)
                {
                    throw new Exception("Can't draw single points, maybe we should, feel free to add code here later when needed");
                }

                if (m_hideBack)
                {
                    double len = (double)points.Length;
                    Point3D average = new Point3D(
                        points.Sum(x => x.X) / len,
                        points.Sum(x => x.Y) / len,
                        points.Sum(x => x.Z) / len);

                    Point3D normal = (points[0] - average).CrossProduct(points[1] - average).Normalized();
                    // when the dot product is > 0 it is a "back plane" (pointing away from the camera)
                    if ((Origin + average - projection.Camera.From).DotProduct(normal) > 0.0)
                    {
                        continue;
                    }
                }

                for (int j = 1; j < points.Length; j++) // skip 1, so that we can draw a line form "-1" to "1"
                {
                    AllLines.Add(new Line3D(points[j -1], points[j]));
                }
                // Draw one from the last line back to the first
                AllLines.Add(new Line3D(points.Last(), points.First()));
            }

            base.Render(projection, imageData, lables);
        }

        private readonly Point3D m_angle = new Point3D();

        private readonly int[][] m_faces;
        private readonly Point3D[] m_points;

        private readonly bool m_spin;
        private readonly bool m_hideBack;

        private const double c_size = 25;
    }
}