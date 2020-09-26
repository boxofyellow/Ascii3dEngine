
namespace Ascii3dEngine
{
    public class Screen
    {
        public Point2D Center;
        public Point2D Size;
        public Screen(Point2D size)
        {
            Size = size;
            Center = new Point2D(Size.H / 2, Size.V / 2);
        }
    }
}