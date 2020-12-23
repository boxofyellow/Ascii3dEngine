namespace Ascii3dEngine
{
    public class InfinitePlane : PolygonActorBase
    {
        public InfinitePlane(Settings settings, double? x = null, double? y = null, double? z = null) 
            : base(settings, GetData(x, y, z)) { }

        // Don't Rotate
        public override void Act(System.TimeSpan timeDelta, System.TimeSpan elapsedRuntime, Camera camera) {}

        private static (Point3D[] Points, int[][] Faces) GetData(double? x, double? y, double? z)
        {
            // ok so maybe they are not infinite
            double min = -Utilities.MaxRange;
            double max = Utilities.MaxRange;

            Point3D[] points = new [] {
                new Point3D(
                        x ?? min,
                        y ?? min,
                        z ?? min),

                new Point3D(
                        x ?? max,
                        // if x is null, we already have one max
                        y ?? ((x == null) ? min : max),
                        z ?? min),

                new Point3D(
                        x ?? max,
                        y ?? max,
                        z ?? max),

                new Point3D(
                        x ?? min,
                        // if z is null, we already have one max
                        y ?? ((z == null) ? min : max),
                        z ?? max),
            };

            System.Console.WriteLine(string.Join(System.Environment.NewLine, points));

            int[][] faces = new [] {new [] {0, 1, 2, 3}};
            return (points, faces);
        }
    }
}