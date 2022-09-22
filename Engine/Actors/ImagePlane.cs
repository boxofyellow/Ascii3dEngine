using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Ascii3dEngine.Engine
{
    public class ImagePlane : PolygonActorBase
    {
        public static ImagePlane Create(string imageFilePath, Point3D center, Point3D normal, Point3D up, double scale = 1.0)
            => new(center,
                   normal,
                   up,
                   scale,
                   GetData(imageFilePath, out Point3D offset, out var colorData),
                   offset,
                   colorData);

        protected ImagePlane(Point3D center, Point3D normal, Point3D up, double scale, (Point3D[] Points, int[][] Faces) polyData, Point3D offset, Argb32[,] colorData) 
            : base(polyData)
        {
            m_offset = offset;
            m_colorData = colorData;
            Motion
                .Alight(fromNormal: Point3D.ZUnit, fromUp: Point3D.YUnit,  normal, up)
                .MoveTo(center)
                .SetScale(new(scale, scale, 1));
        }

        public override bool DoubleSided(Point3D intersection, int id) => true;

        public override ColorProperties ColorAt(Point3D intersection, int id)
        {
            intersection = Motion.Unapply(intersection);
            intersection += m_offset;

            // The Clamp calls here are normally not required, if we did math right it should be fine,
            // however there is the opportunity for rounding shenanigans 
            // So after making changes to this logic we should try it out with 
            // var row = (int)Math.Floor(intersection.Y);
            // var col = (int)Math.Floor(intersection.X);
            var row = Math.Clamp((int)Math.Floor(intersection.Y), 0, m_colorData.GetLength(0) - 1);
            var col = Math.Clamp((int)Math.Floor(intersection.X), 0, m_colorData.GetLength(1) - 1);

            // Row 0 is the top, but normally we would want the top to the rows with the higher number;
            row = m_colorData.GetLength(0) - 1 - row;

            return ColorProperties.Plastic(m_colorData[row, col]);
        }

        protected static (Point3D[] Points, int[][] Faces) GetData(string imageFilePath, out Point3D offset, out Argb32[,] colorData)
        {
            using Image<Argb32> image = Image.Load<Argb32>(imageFilePath);
            var widthOver2 = image.Width / 2.0;
            var hightOver2 = image.Height / 2.0;

            offset = new(widthOver2, hightOver2, 0);

            colorData = image.GetPixelData();

            // start with our image centered on the x-y axes over the origin
            Point3D[] points = new Point3D[] {
                    new(-widthOver2,  hightOver2, 0),
                    new( widthOver2,  hightOver2, 0),
                    new( widthOver2, -hightOver2, 0),
                    new(-widthOver2, -hightOver2, 0),
                };
            // that makes the normal for this The z-unit vector

            int[][] faces = new[] { new[] { 0, 1, 2, 3 } };
            return (points, faces);
        }

        private readonly Point3D m_offset;

        private readonly Argb32[,] m_colorData;

    }
}