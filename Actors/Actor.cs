using System;
using System.Collections.Generic;

namespace Ascii3dEngine
{
    public abstract class Actor
    {
        public Actor() { }

        public virtual void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera) {}

        protected void DrawLine(Projection projection, bool[,] imageData, Point3D start, Point3D end)
        {
            (bool inView, Point2D p1, Point2D p2) = projection.Trans_Line(start, end);
            if (inView)
            {
                imageData.DrawLine(p1, p2);
            }
        }

        public abstract void Render(Projection projection, bool[,] imageData, List<Label> lables);
    }
}