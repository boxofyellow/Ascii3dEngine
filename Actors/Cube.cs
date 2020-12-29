using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascii3dEngine
{
    public class Cube : PolygonActorBase
    {
        public Cube(Settings settings, CharMap map) : base(settings, GetData(), map.UniqueCharLength)
        {
            m_map = map;
            m_ids = new int [m_lables.Length];

            for (int i = 0; i < m_lables.Length; i++)
            {
                m_ids[i] = -1;
                for (int j = 0; j < map.UniqueCharLength; j++)
                {
                    if (map.GetUniqueChar(j + IdsRangeStart) == m_lables[i])
                    {
                        m_ids[i] = j + IdsRangeStart;
                        m_faces[m_ids[i]] = i;
                        break;
                    }
                }
                if (m_ids[i] == -1)
                {
                    throw new Exception($"Well we failed to find one of our lables, {i} {m_lables[i]}");
                }
            }
            m_properties = new [] {
                ColorProperties.RedPlastic,
                ColorProperties.BluePlastic,
                ColorProperties.GreenPlastic,
                ColorProperties.YellowPlastic,
                ColorProperties.PurplePlastic,
                ColorProperties.CyanPlastic,
            };
        }

        private static (Point3D[] Points, int[][] Faces) GetData()
        {
            Point3D[] points = new Point3D[CubeDefinition.Points.Length];

            for (int i = default; i < points.Length; i++)
            {
                points[i] = CubeDefinition.Points[i] * (c_size / 2.0);
            }

            // We need to keep this and m_lables in sync
            int[][] faces = new int[CubeDefinition.Faces.Length][];
            for (int i = default; i < faces.Length; i++)
            {
                faces[i] = CubeDefinition.Faces[i].ToArray();
            }

            return (points, faces);
        }

        public override void AddLabel(int face, Projection projection, Point3D[] points, List<Label> labels)
        { 
            Point3D average = points.Average();
            (bool inView, _, Point2D projectedP2) = projection.Trans_Line(new Point3D(), average);
            if (inView)
            {
                labels.Add(new Label(
                    projectedP2.H / m_map.MaxX,
                    projectedP2.V / m_map.MaxY,
                    m_lables[face]));
            }
        }

        public override ColorProperties ColorAt(Point3D intersection, int id) => m_properties[GetFaceFromId(id)];

        protected override int GetId(int face) => m_ids[face];
        protected override int GetFaceFromId(int id) => m_faces[id];
        private readonly char[] m_lables = new [] {'F', 'B', 'R', 'L', 't', 'b'};
        private readonly ColorProperties[] m_properties;
        private readonly int[] m_ids;

        // Maps Id's back to faces.
        private readonly Dictionary<int, int> m_faces = new Dictionary<int, int>();

        private readonly CharMap m_map;
        private const double c_size = 25;
    }
}