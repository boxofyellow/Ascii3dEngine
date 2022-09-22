using Ascii3dEngine.Engine;
using SixLabors.ImageSharp.PixelFormats;

namespace Ascii3dEngine.TechDemo
{
    public class SpinningImage : ImagePlane
    {
        public static SpinningImage Create(Settings settings, Point3D center, Point3D normal, Point3D up, double scale = 1.0)
            => new(settings,
                    center,
                    normal,
                    up,
                    scale,
                    GetData(settings.ImagePlaneFile!, out Point3D offset, out var colorData),
                    offset,
                    colorData);

        private SpinningImage(Settings settings, Point3D center, Point3D normal, Point3D up, double scale, (Point3D[] Points, int[][] Faces) polyData, Point3D offset, Argb32[,] colorData) 
            : base(center, normal, up, scale, polyData, offset, colorData) => m_spin = settings.Spin;

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

        private readonly bool m_spin;

        private readonly static double c_15degreesRadians = Utilities.DegreesToRadians(15);
    }
}
