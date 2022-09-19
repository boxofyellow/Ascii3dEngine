using System;
using Ascii3dEngine.Engine;

namespace Ascii3dEngine.TechDemo
{
    public class SpinningSphere : ImageSphere
    {
        public SpinningSphere(Settings settings, Point3D center) 
            : base(settings.ImageSphereFile!, center, settings.ImageSphereRadius) => m_spin = settings.Spin;

        public override void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera)
        {
            if (m_spin)
            {
                Motion.RotateByY(timeDelta.TotalSeconds * c_15degreesRadians);
            }

            base.Act(timeDelta, elapsedRuntime, camera);
        }

        private readonly bool m_spin;

        private readonly static double c_15degreesRadians = Utilities.DegreesToRadians(15);
    }
}
