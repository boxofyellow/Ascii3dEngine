namespace Ascii3dEngine.Engine
{
    public class CheckeredInfinitePlane : InfinitePlane
    {
        public CheckeredInfinitePlane(ColorProperties baseProperties, ColorProperties checkedProperties, double? x = null, double? y = null, double? z = null, double scale = 0.05)
            : base(baseProperties, x, y, z)
        {
            m_checkedProperties = checkedProperties;
            m_valueFunction = (x != null) ? (p) => Math.Floor(p.Y * scale) + Math.Floor(p.Z * scale)
                            : (y != null) ? (p) => Math.Floor(p.X * scale) + Math.Floor(p.Z * scale)
                                          : (p) => Math.Floor(p.X * scale) + Math.Floor(p.Y * scale);
        }

        public override ColorProperties ColorAt(Point3D intersection, int id)
            => Math.Abs(m_valueFunction(intersection)) % 2 == 0 ? m_checkedProperties : base.ColorAt(intersection, id);

        private readonly ColorProperties m_checkedProperties;
        private readonly Func<Point3D, double> m_valueFunction;
    }
}