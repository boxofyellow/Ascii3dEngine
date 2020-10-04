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

        // Allows actors to reserve Ids, they will be granted a block count long starting at the returned value
        protected static int ReserveIds(int count)
        {
            lock (m_lockObject)
            {
                if (int.MaxValue - m_lastReserved <= count)
                {
                    throw new OverflowException($"Reserved too many items! {nameof(m_lastReserved)}:{m_lastReserved} {nameof(count)}:{count}");
                }

                int result = m_lastReserved + 1;
                m_lastReserved = result + count;
                return result;
            }
        }

        private static readonly object m_lockObject = new object();
        private static int m_lastReserved = default; // 0 is reserved for "none"
    }
}