using System;
using SixLabors.ImageSharp.PixelFormats;

namespace Ascii3dEngine.Tanks
{
    public class Projectile : SolidSphere
    {
        public static Projectile Create(Settings settings, Scene scene, Point3D center, Rgb24 lightColor, ColorProperties properties, Point3D direction)
        {
            var result = new Projectile(settings, center, lightColor, properties, direction);
            scene.AddActor(result);
            scene.AddLightSource(result.Source);
            return result;
        }

        private Projectile(Settings settings, Point3D center, Rgb24 lightColor, ColorProperties properties, Point3D direction) : base(settings, center, 0.25, properties)
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
}