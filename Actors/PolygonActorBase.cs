using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascii3dEngine
{
    public abstract class PolygonActorBase : Actor
    {
        public PolygonActorBase(Settings settings, (Point3D[] Points, int[][] Faces) data, int? numberOfIdsToreserve = null) : base()
        {
            m_spin = settings.Spin;
            m_hideBack = settings.HideBack;
            m_points = data.Points;
            m_faces = data.Faces;
            IdsRangeStart = ReserveIds(numberOfIdsToreserve ?? m_faces.Length);
        }

        public virtual void AddLabel(int face, Projection projection, Point3D[] points, List<Label> labels) { }

        public override void Act(System.TimeSpan timeDelta, System.TimeSpan elapsedRuntime, Camera camera)
        {
            if (m_spin)
            {
                double delta = timeDelta.TotalSeconds * 15.0;
                delta %= 360.0;
                if (delta < 0) delta += 360.0;
                Point3D deltaAngle = new Point3D(delta, -delta, delta / 2.0);
                for (int i = default; i < m_points.Length; i++)
                {
                    m_points[i] = m_points[i].Rotate(deltaAngle);
                }
                m_areCachesDirty = true;
            }
        }

        public override void Render(Projection projection, bool[,] imageData, List<Label> labels)
        {
            for (int i = default; i < m_faces.Length; i++)
            {
                Point3D[] points = m_faces[i]
                    .Select(x => m_points[x])
                    .ToArray();

                if (points.Length < 3)
                {
                    throw new Exception("Can't draw single points/lines, maybe we should, feel free to add code here later when needed");
                }

                if (m_hideBack)
                {
                    Point3D normal = (points[1] - points[0]).CrossProduct(points[2] - points[0]).Normalized();
                    // when the dot product is > 0 it is a "back plane" (pointing away from the camera)
                    if ((points[0] - projection.Camera.From).DotProduct(normal) > 0.0)
                    {
                        continue;
                    }
                }

                AddLabel(i, projection, points, labels);

                for (int j = 1; j < points.Length; j++) // skip 1, so that we can draw a line form "-1" to "1"
                {
                    imageData.DrawLine(projection, points[j - 1], points[j]);
                }
                // Draw one from the last line back to the first
                imageData.DrawLine(projection, points.Last(), points.First());
            }
        }

        public override void StartRayRender(Point3D from)
        {
            m_independentCache ??= new PolygonActorOriginIndependentCache(m_points, m_faces);
            m_dependentCache ??= new PolygonActorOriginDependentCache(m_points, m_faces);

            if (m_areCachesDirty)
            {
                m_independentCache.Update();
            }
            m_dependentCache.Update(from, m_areCachesDirty, m_independentCache);

            m_areCachesDirty = false;
        }

        public override (double DistranceProxy, int Id) RenderRay(Point3D from, Point3D vector, double currentMinDistanceProxy)
        {
            int id = default;

            if (m_independentCache.DoesVestorIntersect(from, vector, currentMinDistanceProxy, m_dependentCache))
            {
                int index;
                (currentMinDistanceProxy, index) = m_dependentCache.FindClosestIntersection(vector, currentMinDistanceProxy, m_independentCache);
                if (index > -1)
                {
                    id = GetId(index);
                }
            }

            return (currentMinDistanceProxy, id);
        }

        protected virtual int GetId(int face) => IdsRangeStart + face;

        protected readonly int IdsRangeStart;

        private bool m_areCachesDirty = true;

        private PolygonActorOriginDependentCache m_dependentCache;
        private PolygonActorOriginIndependentCache m_independentCache;

        private readonly int[][] m_faces;
        private readonly Point3D[] m_points;

        private readonly bool m_spin;
        private readonly bool m_hideBack;
    }
}