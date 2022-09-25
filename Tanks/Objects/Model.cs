using System.Reflection;
using SixLabors.ImageSharp.PixelFormats;

public class Model : PolygonActorBase
{
    public Model(string fileName, Point3D center, Rgb24 color) 
        : base(WaveObjFormParser.Parse(Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "Data",
            fileName)))
        {
            m_properties = ColorProperties.Plastic(color);
            Motion.MoveTo(center);
        } 

    public override ColorProperties ColorAt(Point3D intersection, int id) => m_properties;

    private readonly ColorProperties m_properties;
}