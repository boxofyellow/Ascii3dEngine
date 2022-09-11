namespace Ascii3dEngine.Engine
{
    public class SolidSphere : Sphere
    {
        public SolidSphere(Settings settings, Point3D center, double radius, ColorProperties properties) 
            : base(settings, center, radius) => m_properties = properties;

        public override ColorProperties ColorAt(Point3D intersection, int id) => m_properties;

        private readonly ColorProperties m_properties;
    }
}