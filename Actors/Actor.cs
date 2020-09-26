using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascii3dEngine
{
    public class Actor
    {
        public Actor(Point3D? origin = default) => Origin = origin ?? new Point3D();

        public Point3D Origin { get; set; }
        public IEnumerable<(Point3D Start, Point3D End)> RenderLines => AllLines.Select(x => (Origin + x.Start, Origin + x.End));
        protected readonly List<Line3D> AllLines = new List<Line3D>();
        public virtual void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera) {}
        public virtual void Render(Projection projection, bool[,] imageData, List<Label> lables)
        {
            foreach (Line3D line in AllLines)
            {
                (bool inView, Point2D p1, Point2D p2) = projection.Trans_Line(Origin + line.Start, Origin + line.End);
                if (inView)
                {
                    imageData.DrawLine(p1, p2);
                }
            }
        }
    }
}