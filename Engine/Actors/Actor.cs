public abstract class Actor
{
    public Actor() { }

    public virtual void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera) {}

    public virtual void StartRayRender(Point3D from, LightSource[] sources) {}

    public virtual (double DistanceProxy, int Id, Point3D Intersection) RenderRay(Point3D from, Point3D vector, double currentMinDistanceProxy) => default;

    public virtual (double DistanceProxy, bool Hit, Point3D Intersection) RenderRayForId(int id, Point3D from, Point3D vector) => default;

    public virtual ColorProperties ColorAt(Point3D intersection, int id) => ColorProperties.RedPlastic;

    public abstract Point3D NormalAt(Point3D intersection, int id);

    public virtual bool DoubleSided(Point3D intersection, int id) => false;

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
    public virtual object GetTrackingObjectFromId(int id) => id;
#endif

    private static readonly object s_lockObject = new();
    private static int s_lastReserved = default; // 0 is reserved for "none"
}