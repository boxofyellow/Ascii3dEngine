using SixLabors.ImageSharp.PixelFormats;

public class Projectile : SolidSphere
{
    public static Projectile Create(Scene scene, Point3D center, Rgb24 lightColor, Point3D direction)
    {
        var result = new Projectile(center, lightColor, direction);
        scene.AddActor(result);
        scene.AddLightSource(result.Source);
        return result;
    }

    private Projectile(Point3D center, Rgb24 lightColor, Point3D direction) 
        : base(center, 0.25, ColorProperties.Plastic(lightColor))
    {
        m_start = center;
        m_direction = direction;
        Source = new LightSource(center, lightColor);
    }

    public override void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera)
    {
        Center += m_direction * timeDelta.TotalSeconds;
        Source.Point = Center;
    }

    public void Rest() => Center = m_start;

    public override bool DoesItCastShadow(int sourceIndex, Point3D from, Point3D vector, int minId) => false;

    public override bool DoubleSided(Point3D intersection, int id) => true;

    private readonly LightSource Source;
    private readonly Point3D m_direction;
    private readonly Point3D m_start;
}