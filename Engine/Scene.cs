using System;
using System.Collections.Generic;
using SixLabors.ImageSharp.PixelFormats;

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

        public void AddLightSource(LightSource source) => m_lightSources.Add(source);

        public void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime)
        {
            foreach (Actor actor in m_actors)
            {
                actor.Act(timeDelta, elapsedRuntime, Camera);
            }
            foreach  (LightSource source in m_lightSources)
            {
                source.Act(timeDelta, elapsedRuntime, Camera);
            }
        }

        public (bool[,] ImageData, List<Label> Labels) Render()
        {
            Projection projection = new Projection(Camera, Screen);
            bool[,] imageData = new bool[Screen.Size.H, Screen.Size.V];
            List<Label> labels = new List<Label>();

            if (m_settings.UseRay)
            {
                RayTracer.Trace(m_settings, imageData, this, projection, m_actors);
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
                RayTracer.TraceCharRay(m_settings, Utilities.Ratio(size.H, map.MaxX), Utilities.Ratio(size.V, map.MaxY), this, map, m_actors),
                new List<Label>()
            );
        }

        public Rgb24[,] RenderCharRayColor(Point2D size, CharMap map)
        {
            return RayTracer.TraceColor(m_settings, Utilities.Ratio(size.H, map.MaxX), Utilities.Ratio(size.V, map.MaxY), this, map, m_actors);
        }


        private readonly Settings m_settings;

        private readonly List<Actor> m_actors = new List<Actor>();
        private readonly List<LightSource> m_lightSources = new List<LightSource>();
    }
}