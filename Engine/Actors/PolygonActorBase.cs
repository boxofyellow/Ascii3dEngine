namespace Ascii3dEngine.Engine
{
    public abstract class PolygonActorBase : Actor
    {
        public PolygonActorBase((Point3D[] Points, int[][] Faces) data, int? numberOfIdsToReserve = null) : base()
        {
            m_points = data.Points;
            m_startPoints = new Point3D[data.Points.Length];
            Array.Copy(data.Points, m_startPoints, data.Points.Length);
            m_faces = data.Faces;
            IdsRangeStart = ReserveIds(numberOfIdsToReserve ?? m_faces.Length);
        }

        public override void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera)
        {
            if (!Motion.IsIdentity)
            {
                for (int i = default; i < m_startPoints.Length; i++)
                {
                    // Incrementally updating the points (aka not keeping the starting position and just moving them a little bit each frame) has some draw backs
                    // doing that results in small rounding problems.  These are not really noticeable until you attempt to undo them...
                    // So by effectively restarting from the start will helping us undo the rotation so that we can map an intersection back with some original data.
                    m_points[i] = Motion.Apply(m_startPoints[i]);
                }
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

            bool areCachesDirty = m_motionMatrixVersion != Motion.Version;
            if (areCachesDirty)
            {
                m_independentCache.Update();
            }
            m_dependentCache.Update(from, areCachesDirty, m_independentCache);

            for (int i = 0; i < sources.Length; i++)
            {
                (m_lightDependentCache[i] ??= new(m_points, m_faces))
                    .Update(sources[i].Point, areCachesDirty, m_independentCache);
            }

            m_motionMatrixVersion = Motion.Version;
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

        protected MotionMatrix Motion = new();
        private int m_motionMatrixVersion = MotionMatrix.VersionUninitialized;

        private PolygonActorOriginDependentCache? m_dependentCache;
        private PolygonActorOriginIndependentCache? m_independentCache;
        private PolygonActorOriginDependentCache[]? m_lightDependentCache;

        private readonly int[][] m_faces;

        // these get updated with our rotation logic.
        private readonly Point3D[] m_points;

        // These remain unchanged
        private readonly Point3D[] m_startPoints;
    }
}