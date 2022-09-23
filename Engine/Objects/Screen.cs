public class Screen
{
    public Screen(Point2D size)
    {
        Size = size;
        Center = new Point2D(Size.H / 2, Size.V / 2);
    }

    public readonly Point2D Center;
    public readonly Point2D Size;
}