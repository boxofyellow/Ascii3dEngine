using System;
using System.Collections.Generic;

namespace Ascii3dEngine
{
    public abstract class Actor
    {
        public Actor() { }

        public virtual void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera) {}

        public abstract void Render(Projection projection, bool[,] imageData, List<Label> labels);

        public virtual void StartRayRender(Point3D from) {}

        public virtual (double DistranceProxy, int Id) RenderRay(Point3D from, Point3D vector, double currentMinDistanceProxy) => default;

        // Allows actors to reserve Ids, they will be granted a block count long starting at the returned value
        protected static int ReserveIds(int count)
        {
            lock (m_lockObject)
            {
                if (int.MaxValue - LastReserved <= count)
                {
                    throw new OverflowException($"Reserved too many items! {nameof(LastReserved)}:{LastReserved} {nameof(count)}:{count}");
                }

                int result = LastReserved + 1;
                LastReserved = result + count;
                return result;
            }
        }

        private static readonly object m_lockObject = new object();
        public static int LastReserved {get; private set;} = default;// 0 is reserved for "none"
    }
}