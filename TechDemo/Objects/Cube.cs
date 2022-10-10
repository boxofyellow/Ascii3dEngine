public class Cube : PolygonActorBase
{
    public Cube(Settings settings, CharMap map) : base(GetData(), map.UniqueCharLength)
    {
        m_spin = settings.Spin;
        m_ids = new int [s_properties.Length];

        for (int i = 0; i < s_properties.Length; i++)
        {
            m_ids[i] = i + IdsRangeStart;
            m_faces[m_ids[i]] = i;
        }
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

    public override ColorProperties ColorAt(Point3D intersection, int id) => s_properties[GetFaceFromId(id)];

    protected override int GetId(int face) => m_ids[face];
    protected override int GetFaceFromId(int id) => m_faces[id];
    private static readonly ColorProperties[] s_properties = new [] {
            ColorProperties.RedPlastic,
            ColorProperties.BluePlastic,
            ColorProperties.GreenPlastic,
            ColorProperties.YellowPlastic,
            ColorProperties.PurplePlastic,
            ColorProperties.CyanPlastic,
        };

    private readonly int[] m_ids;

    // Maps Id's back to faces.
    private readonly Dictionary<int, int> m_faces = new();

    private const double c_size = 25;

    private readonly bool m_spin;
    private readonly static double c_15degreesRadians = Utilities.DegreesToRadians(15);
}