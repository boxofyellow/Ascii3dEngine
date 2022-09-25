using SixLabors.ImageSharp.PixelFormats;

public class Tank : Player
{
    public static Tank Create(Scene scene, Point3D center, Rgb24 color)
    {
        var turret = new Model("tank_turret.obj", center, color);
        var result = new Tank(center, color, turret);

        scene.AddActor(result);
        scene.AddActor(turret);

        return result;
    }

    private Tank(Point3D center, Rgb24 color, Model turret) : base(center, color)
    {
        m_color = color;
        m_turret = turret;
    }

    public override void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera)
    {
        base.Act(timeDelta, elapsedRuntime, camera);
    }

    private readonly Rgb24 m_color;
    private readonly Model m_turret;
}