using System;
using System.Collections.Generic;

namespace Ascii3dEngine
{
    public class Scene
    {

        public Scene(Settings settings, Point2D size)
        {
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
            bool[,] imageData = new bool[(int)Screen.Size.H, (int)Screen.Size.V];
            List<Label> labels = new List<Label>();
            foreach (Actor actor in m_actors)
            {
                actor.Render(projection, imageData, labels);
            }
            return (imageData, labels);
        }

        private List<Actor> m_actors = new List<Actor>();
    }
}