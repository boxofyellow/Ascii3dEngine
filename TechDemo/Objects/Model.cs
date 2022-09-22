using Ascii3dEngine.Engine;

namespace Ascii3dEngine.TechDemo
{
    public class Model : PolygonActorBase
    {
        public Model(Settings settings) 
            : base(WaveObjFormParser.Parse(settings.ModelFile!)) => m_spin = settings.Spin;

        public override void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera)
        {
            if (m_spin)
            {
                var delta = timeDelta.TotalSeconds * c_15degreesRadians;

                Motion
                    .RotateByX(delta)
                    .RotateByY(delta)
                    .RotateByZ(-delta / 2.0);
            }

            base.Act(timeDelta, elapsedRuntime, camera);
        }

        private readonly bool m_spin;
        private readonly static double c_15degreesRadians = Utilities.DegreesToRadians(15);
    }
}