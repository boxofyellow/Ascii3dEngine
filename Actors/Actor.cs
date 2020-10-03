using System;
using System.Collections.Generic;

namespace Ascii3dEngine
{
    public abstract class Actor
    {
        public Actor() { }

        public virtual void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera) {}

        public abstract void Render(Projection projection, bool[,] imageData, List<Label> lables);

        public virtual void StartRayRender() {}

        public virtual (double Distrance, int Id) RenderRay(Point3D from, Point3D vector) => (default, default);
    }
}