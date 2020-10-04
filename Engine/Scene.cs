using System;
using System.Collections.Generic;

namespace Ascii3dEngine
{
    public class Scene
    {

        public Scene(Settings settings, Point2D size)
        {
            m_settings = settings;
            Screen = new Screen(size);
            Camera = new Camera(settings);
        }

        public readonly Camera Camera;
        public readonly Screen Screen;

        public void AddActor(Actor actor) => m_actors.Add(actor);

        public void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime)
        {
            foreach (Actor actor in m_actors)
            {
                actor.Act(timeDelta, elapsedRuntime, Camera);
            }
        }

        public (bool[,] ImageData, List<Label> Labels) Render()
        {
            Projection projection = new Projection(Camera, Screen);
            bool[,] imageData = new bool[Screen.Size.H, Screen.Size.V];
            List<Label> labels = new List<Label>();

            if (m_settings.UseRay)
            {
                RayTracer.Trace(imageData, this, projection, m_actors);
            }
            else
            {
                foreach (Actor actor in m_actors)
                {
                    actor.Render(projection, imageData, labels);
                }
            }

            return (imageData, labels);
        }

        public (string[] Lines, List<Label> Labels) RenderCharRay(Point2D size, CharMap map)
        {
            return (
                RayTracer.TraceCharRay(size.H / map.MaxX , size.V / map.MaxY, this, map, m_actors),
                new List<Label>()
            );
        }

        private readonly Settings m_settings;

        private readonly List<Actor> m_actors = new List<Actor>();
    }
}