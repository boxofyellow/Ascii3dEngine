using Ascii3dEngine.Engine;

namespace Ascii3dEngine.TechDemo
{
    public class Cube : PolygonActorBase
    {
        public Cube(Settings settings, CharMap map) : base(GetData(), map.UniqueCharLength)
        {
            m_spin = settings.Spin;
            m_ids = new int [m_labels.Length];

            for (int i = 0; i < m_labels.Length; i++)
            {
                m_ids[i] = -1;
                for (int j = 0; j < map.UniqueCharLength; j++)
                {
                    if (map.GetUniqueChar(j + IdsRangeStart) == m_labels[i])
                    {
                        m_ids[i] = j + IdsRangeStart;
                        m_faces[m_ids[i]] = i;
                        break;
                    }
                }
                if (m_ids[i] == -1)
                {
                    throw new Exception($"Well we failed to find one of our labels, {i} {m_labels[i]}");
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

        public override void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera)
        {
            if (m_spin)
            {
                var delta = timeDelta.TotalSeconds * c_15degreesRadians;

                Motion
                    .RotateByX(delta)
                    .RotateByY(delta)
                    .RotateByZ(-delta / 2.0);
            }

            base.Act(timeDelta, elapsedRuntime, camera);
        }

        private static (Point3D[] Points, int[][] Faces) GetData()
        {
            var points = new Point3D[CubeDefinition.Points.Length];

            for (int i = default; i < points.Length; i++)
            {
                points[i] = CubeDefinition.Points[i] * (c_size / 2.0);
            }

            // We need to keep this and m_labels in sync
            int[][] faces = new int[CubeDefinition.Faces.Length][];
            for (int i = default; i < faces.Length; i++)
            {
                faces[i] = CubeDefinition.Faces[i].ToArray();
            }

            return (points, faces);
        }

        public override ColorProperties ColorAt(Point3D intersection, int id) => m_properties[GetFaceFromId(id)];

        protected override int GetId(int face) => m_ids[face];
        protected override int GetFaceFromId(int id) => m_faces[id];
        private readonly char[] m_labels = new [] {'F', 'B', 'R', 'L', 't', 'b'};
        private readonly ColorProperties[] m_properties;
        private readonly int[] m_ids;

        // Maps Id's back to faces.
        private readonly Dictionary<int, int> m_faces = new();

        private const double c_size = 25;

        private readonly bool m_spin;
        private readonly static double c_15degreesRadians = Utilities.DegreesToRadians(15);
    }
}