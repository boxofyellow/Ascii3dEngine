using SixLabors.ImageSharp.PixelFormats;

public class Player : Model
{
    public Player(Point3D center, Rgb24 color)
        : base("tank_hull.obj", center, color)
        => m_color = color;

    public override void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera)
    {
        base.Act(timeDelta, elapsedRuntime, camera);
    }

    private readonly Rgb24 m_color;
}