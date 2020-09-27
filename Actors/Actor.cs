using System;
using System.Collections.Generic;

namespace Ascii3dEngine
{
    public abstract class Actor
    {
        public Actor() { }

        public virtual void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera) {}

        public abstract void Render(Projection projection, bool[,] imageData, List<Label> lables);
    }
}