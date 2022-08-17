using System;

namespace Ascii3dEngine
{
    public abstract class PolygonActorBase : Actor
    {
        public PolygonActorBase(Settings settings, (Point3D[] Points, int[][] Faces) data, int? numberOfIdsToReserve = null) : base()
        {
            m_spin = settings.Spin;
            m_points = data.Points;
            m_faces = data.Faces;
            IdsRangeStart = ReserveIds(numberOfIdsToReserve ?? m_faces.Length);
        }

        public override void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera)
        {
            if (m_spin)
            {
                double delta = timeDelta.TotalSeconds * 15.0;
                delta %= 360.0;
                if (delta < 0) delta += 360.0;
                var deltaAngle = new Point3D(delta, -delta, delta / 2.0);
                for (int i = default; i < m_points.Length; i++)
                {
                    m_points[i] = m_points[i].Rotate(deltaAngle);
                }
                m_areCachesDirty = true;
            }
        }

        public override void StartRayRender(Point3D from, LightSource[] sources)
        {
            m_independentCache ??= new(m_points, m_faces);
            m_dependentCache ??= new(m_points, m_faces);

            if (m_lightDependentCache == null || m_lightDependentCache.Length != sources.Length)
            {
                m_lightDependentCache = new PolygonActorOriginDependentCache[sources.Length];
            }

            if (m_areCachesDirty)
            {
                m_independentCache.Update();
            }
            m_dependentCache.Update(from, m_areCachesDirty, m_independentCache);

            for (int i = 0; i < sources.Length; i++)
            {
                (m_lightDependentCache[i] ??= new(m_points, m_faces))
                    .Update(sources[i].Point, m_areCachesDirty, m_independentCache);
            }

            m_areCachesDirty = false;
        }

        public override (double DistanceProxy, int Id, Point3D Intersection) RenderRay(Point3D from, Point3D vector, double currentMinDistanceProxy)
        {
            int id = default;
            Point3D intersection = default;

            if (m_independentCache!.DoesVectorIntersect(from, vector, currentMinDistanceProxy, m_dependentCache!))
            {
                int index;
                (currentMinDistanceProxy, index, intersection) = m_dependentCache!.FindClosestIntersection(vector, currentMinDistanceProxy, m_independentCache);
                if (index > -1)
                {
                    id = GetId(index);
                }
            }

            return (currentMinDistanceProxy, id, intersection);
        }

        public override (double DistanceProxy, bool Hit, Point3D Intersection) RenderRayForId(int id, Point3D from, Point3D vector) 
            => m_dependentCache!.FindIntersectionForIndex(
                    GetFaceFromId(id),
                    vector,
                    m_independentCache!);

        public override bool DoesItCastShadow(int sourceIndex, Point3D from, Point3D vector, int minId)
        {
            // the vector we are given is from the light source to point of intersection, so distance > 1 means it is NOT casting a shadow here
            const double currentMinDistanceProxy = 1.0;
            int indexToIgnore = minId - IdsRangeStart;

            if (m_independentCache!.DoesVectorIntersect(from, vector, currentMinDistanceProxy, m_lightDependentCache![sourceIndex]))
            {
                if (m_lightDependentCache[sourceIndex].IsIntersectionWithInOne(vector, indexToIgnore, m_independentCache))
                {
                    return true;
                }
            }
            return false;
        }

        public override Point3D NormalAt(Point3D intersection, int id) => m_independentCache!.CachedNormals[GetFaceFromId(id)];

        protected virtual int GetId(int face) => IdsRangeStart + face;

        protected virtual int GetFaceFromId(int id) => id - IdsRangeStart;

        protected readonly int IdsRangeStart;

        private bool m_areCachesDirty = true;

        private PolygonActorOriginDependentCache? m_dependentCache;
        private PolygonActorOriginIndependentCache? m_independentCache;
        private PolygonActorOriginDependentCache[]? m_lightDependentCache;

        private readonly int[][] m_faces;
        private readonly Point3D[] m_points;

        private readonly bool m_spin;
    }
}