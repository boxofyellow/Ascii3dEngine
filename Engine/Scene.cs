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
            Screen = new(size);
            Camera = new(settings);
        }

        public readonly Camera Camera;
        public readonly Screen Screen;

        public void AddActor(Actor actor) => m_actors.Add(actor);

        public void AddLightSource(LightSource source) => m_lightSources.Add(source);

        public void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime)
        {
            foreach (var actor in m_actors)
            {
                actor.Act(timeDelta, elapsedRuntime, Camera);
            }
            foreach  (var source in m_lightSources)
            {
                source.Act(timeDelta, elapsedRuntime, Camera);
            }
        }

        public Rgb24[,] RenderCharRayColor(Point2D size, CharMap map) 
            => RayTracer.TraceColor(m_settings, Utilities.Ratio(size.H, map.MaxX), Utilities.Ratio(size.V, map.MaxY), this, m_actors, m_lightSources);


        private readonly Settings m_settings;

        private readonly List<Actor> m_actors = new();
        private readonly List<LightSource> m_lightSources = new();
    }
}