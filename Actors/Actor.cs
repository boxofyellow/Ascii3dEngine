using System;
using System.Collections.Generic;

namespace Ascii3dEngine
{
    public abstract class Actor
    {
        public Actor() { }

        public virtual void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera) {}

        public abstract void Render(Projection projection, bool[,] imageData, List<Label> labels);

        public virtual void StartRayRender(Point3D from, LightSource[] sources) {}

        public virtual (double DistranceProxy, int Id, Point3D Intersection) RenderRay(Point3D from, Point3D vector, double currentMinDistanceProxy) => default;

        public virtual ColorProperties ColorAt(Point3D intersection, int id)
        {
            const int maxColor = 1 + (int)byte.MaxValue;
            const int maxColorValue = maxColor * maxColor;
            
            // emulate plastic
            const double diffuse = 0.55;
            const double factor = diffuse / (double)maxColor;

            int value = id * maxColorValue / s_lastReserved;

            double green = (double)(value / maxColor) * factor;
            double blue = (double)(value % maxColor) * factor;

            // Emulate plastic 
            return new ColorProperties(
                new Point3D(0.0, 0.0, 0.0),
                new Point3D(x /*aka Red*/: diffuse, green, blue),
                new Point3D(0.7, 0.7, 0.7),
                32);
        }

        public virtual bool DoesItCastShadow(int sourceIndex, Point3D from, Point3D vector, int minId) => false;

        // Allows actors to reserve Ids, they will be granted a block count long starting at the returned value
        protected static int ReserveIds(int count)
        {
            lock (s_lockObject)
            {
                if (int.MaxValue - s_lastReserved <= count)
                {
                    throw new OverflowException($"Reserved too many items! {nameof(s_lastReserved)}:{s_lastReserved} {nameof(count)}:{count}");
                }

                int result = s_lastReserved + 1;
                s_lastReserved = result + count;
                return result;
            }
        }

#if (DEBUG)
        public virtual object GetTrackingObjectFromId(int id) => default;
#endif

        private static readonly object s_lockObject = new object();
        private static int s_lastReserved = default; // 0 is reserved for "none"
    }
}