using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace Ascii3dEngine.Engine
{
    public class ImageSphere : Sphere
    {
        public ImageSphere(string imageFilePath, Point3D center, double radius) : base(center, radius)
        {
            using Image<Argb32> image = Image.Load<Argb32>(imageFilePath);

            m_colorData = new Argb32[image.Height][];
            for (int i = 0; i < image.Height; i++)
            {
                // Row 0 is the top, but normally we would want the top to the rows with the higher number;
                m_colorData[i] = image.GetPixelRowSpan(image.Height - 1 - i).ToArray();
            }
        }

        public override ColorProperties ColorAt(Point3D intersection, int id)
        {
            const double piOver4 = Math.PI / 4.0;
            const double piOver2 = Math.PI / 2.0;

            intersection = Motion.Unapply(intersection);

            //https://stackoverflow.com/questions/5674149/3d-coordinates-on-a-sphere-to-latitude-and-longitude
            // we already unapplied out motion matrix (which undid the scalding), which will map our points on a sphere centered at the origin with a radius of 1
            // r = √(x² + y² + z²) = 1
            // θ = cos-1(z/r), (90° - θ) your latitude (negative means it's on the bottom) as it's measured from top.
            // 𝜑 = tan-1(x/y)
            // But for their coordinate system Z was "up" ∴ we will need to swap Y and Z

            var latitude = Math.Acos(intersection.Y) - piOver2;
            var longitude = Math.Atan2(intersection.X, intersection.Z);

            // https://en.wikipedia.org/wiki/Web_Mercator_projection
            var x = longitude + Math.PI;                                         // x = λ + Math.PI
            var y = Math.PI - Math.Log(Math.Tan(piOver4 + latitude / 2.0));      // y = π - ln(tan(π/4 + 𝜑 / 2))
            // This should yield 0 ≤ x ≤ 2π and 0 ≤ y ≤ 2π
            
            // It did mention a 𝜑MAX (as 2 * tan-1(𝑒^π) - π/2), but that never needed, But it looks like that gets managed with the Clamp here
            int row = Math.Clamp((int)Math.Floor(y * (double)m_colorData.Length / Math.Tau), 0, m_colorData.Length - 1);
            int col = Math.Clamp((int)Math.Floor(x * (double)m_colorData[0].Length / Math.Tau), 0, m_colorData[0].Length - 1);

            return ColorProperties.Plastic(m_colorData[row][col]);
        }

        private readonly Argb32[][] m_colorData;
    }
}