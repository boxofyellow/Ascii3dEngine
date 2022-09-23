using System.Runtime.CompilerServices;

/// <summary>
/// This is used to cache data the independent of origin of the ray, as a result this can be reused for the original ray as well as Light/shadow calculations 
/// </summary>
public sealed class PolygonActorOriginIndependentCache
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PolygonActorOriginIndependentCache(Point3D[] points, int[][] faces)
    {
        m_points = points;
        m_faces = faces;
        EdgePoints = new Point3D[CubeDefinition.Faces.Length];
        CachedNormals = new Point3D[m_faces.Length];

        m_cachedMins = new Point3D[m_faces.Length];
        m_cachedMaxes = new Point3D[m_faces.Length];
        m_cachedDrops = new int[m_faces.Length];
        m_cachedVertex0s = new double[m_faces.Length][];
        m_cachedVertex1s = new double[m_faces.Length][];
    }

    public void Update()
    {
        m_globalMinX = double.MaxValue;
        m_globalMinY = double.MaxValue;
        m_globalMinZ = double.MaxValue;
        m_globalMaxX = double.MinValue;
        m_globalMaxY = double.MinValue;
        m_globalMaxZ = double.MinValue;

        int index = 0;
        foreach (int[] pointIndexes in m_faces)
        {
            var p1 = m_points[pointIndexes[0]];
            var v1 = m_points[pointIndexes[1]] - p1;
            var v2 = m_points[pointIndexes[2]] - p1;
            var normal = v1.CrossProduct(v2);
            CachedNormals[index] = normal;

            //
            // The above values help us compute where the ray will intersect with the plane that the face is on.
            // That alone is not enough, we need to determine if that intersection point will be within the polygon defined by the face
            // Since we know the intersection point and the face share the same plane, so lets collect more info for this face
            // that we can then use for each of the many-many rays we are going to test

            //
            // Knowing the min/max of each point of the face will help us very coarsely separate point that are clearly not on the face, and those that will require more work 
            // As Pointed out here https://stackoverflow.com/questions/217578/how-can-i-determine-whether-a-2d-point-is-within-a-polygon/16261774#16261774
            // this check can slow things down in the case that most of the points are within the face
            // But that is unlikely to be the case.  Plus we can compute this once, and reuse it.
            double minX = p1.X, minY = p1.Y, minZ = p1.Z;
            double maxX = minX, maxY = minY, maxZ = minZ;
            for (int i = 1; i < pointIndexes.Length; i++)
            {
                var p = m_points[pointIndexes[i]];
                minX = Math.Min(minX, p.X);
                minY = Math.Min(minY, p.Y);
                minZ = Math.Min(minZ, p.Z);

                maxX = Math.Max(maxX, p.X);
                maxY = Math.Max(maxY, p.Y);
                maxZ = Math.Max(maxZ, p.Z);
            }

            // help deal with rounding
            maxX += 1;
            maxY += 1;
            maxZ += 1;
            minX -= 1;
            minY -= 1;
            minZ -= 1;

            m_cachedMins[index] = new(minX, minY, minZ);
            m_cachedMaxes[index] = new(maxX, maxY, maxZ);

            m_globalMinX = Math.Min(m_globalMinX, minX);
            m_globalMinY = Math.Min(m_globalMinY, minY);
            m_globalMinZ = Math.Min(m_globalMinZ, minZ);

            m_globalMaxX = Math.Max(m_globalMaxX, maxX);
            m_globalMaxY = Math.Max(m_globalMaxY, maxY);
            m_globalMaxZ = Math.Max(m_globalMaxZ, maxZ);

            //
            // We will use pnpoly algorithm to make the final call to tell if the point is in or out.
            // But that algorithm is desired to work in 2D, not 3D space.
            // But we know the all the 3D points being checked are all in the same plane, so we can simply drop one dimensions.
            // As details here https://wrfranklin.org/Research/Short_Notes/pnpoly.html#3D%20Polygons we should try to pick that be impacted by rounding the least
            // I had originally impended the the suggestion of the one with the smallest range (difference between the max and min value of that dimension of each point)
            // But I found that doing did not work so well.
            // I was able to consistently reproduce problems where large portions of a face would behave as if the points did not land on the plane
            // And example may help
            //   Consider a rectangle that is 900 (horizontals) × 553 (vertically), centered at the origin on the X-Y plane
            //   Then rotate that around the Y-axes by 45°
            //   MinX/MaxX (and MinZ/MaxZ) -> ∓ (900/2) * cos(45°) ∴ RangeX/RangeZ > 636
            //   MinX/MinY -> ∓ (553/2) ∴ 553
            //   And since RangeY is smallest it would be dropped
            //   This would reduce the pnpoly algorithm to affectively trying to check if the candidate is on the lines ~(-636, -636)->(636, 636)...  
            // I have found that instead of doing that dropping the dimension that contributes most to the normal avoids that problem
            // Doing removes as much of the approximation introduced by dropping one of the dimensions
            var absX = Math.Abs(normal.X);
            var absY = Math.Abs(normal.Y);
            var absZ = Math.Abs(normal.Z);

            var drop = absX >= absY && absX >= absZ ? 0 // Drop X
                        : absY >= absX && absY >= absZ ? 1 // Drop Y
                        : 2;                               // Drop Z
            m_cachedDrops[index] = drop;

            double[] vertex0s = m_cachedVertex0s[index];
            double[] vertex1s = m_cachedVertex1s[index];
            if (vertex0s == null || vertex0s.Length != pointIndexes.Length)
            {
                vertex0s = new double[pointIndexes.Length];
                m_cachedVertex0s[index] = vertex0s;

                vertex1s = new double[pointIndexes.Length];
                m_cachedVertex1s[index] = vertex1s;
            }

            for (int i = 0; i < pointIndexes.Length; i++)
            {
                var p = m_points[pointIndexes[i]];

                // This looks a little cryptic... so a table may help make sure we are clear
                //  drop   | v0 | v1 
                //  0 -> X |  Y |  Z
                //  1 -> Y |  X |  Z
                //  2 -> Z |  X |  Y
                // If we are dropping X, use Y
                vertex0s[i] = drop == 0 ? p.Y : p.X;
                // If we are dropping Z, use Y
                vertex1s[i] = drop == 2 ? p.Y : p.Z;
            }

            index++;
        }

        for (int i = default; i < EdgePoints.Length; i++)
        {
            var p = CubeDefinition.Points[i];
            EdgePoints[i] = new(
                p.X > 0 ? m_globalMaxX : m_globalMinX,
                p.Y > 0 ? m_globalMaxY : m_globalMinY,
                p.Z > 0 ? m_globalMaxZ : m_globalMinZ);
        }

        // help deal with rounding
        // Doing this after setting up EdgePoints, means we will do our detailed check  when we are close to the edge
        m_globalMaxX += 0.1;
        m_globalMaxY += 0.1;
        m_globalMaxZ += 0.1;
        m_globalMinX -= 0.1;
        m_globalMinY -= 0.1;
        m_globalMinZ -= 0.1;
    }

    public bool DoesVectorIntersect(Point3D from, Point3D vector, double currentMinDistanceProxy,  PolygonActorOriginDependentCache dependentCache)
    {
        bool hitBehind = default;
        bool hitBeyond = default;  // hit found that is beyond currentMinDistanceProxy

        for (int index = default; index < CubeDefinition.Faces.Length; index++)
        {

#if (DEBUG)
            if (DebugUtilities.DebugObject(index))
            {
                // Just another place to add a break point
            }
#endif

            // FYI if you are looking for info about the math in this loop, first check loop below over faces.
            // We are basically doing the same thing here except we are trying to determine if our ray intersect with any side of a rectangular prism that encloses our actor's faces.
            // We can take some short cuts here since the rectangular prism is is perpendicular/parallel with the 3 coronal axes, so just checking min/maxes is good enough 
            var normal = CubeDefinition.Normals[index];
            double denominator = (normal.X * vector.X) + (normal.Y * vector.Y) + (normal.Z * vector.Z);
            if (denominator != 0)
            {
                double numerator = dependentCache.EdgeCachedNumerators[index];
                double t = numerator / denominator;
                if (t < 0)
                {
                    hitBehind = true;
                    if (hitBeyond)
                    {
                        return true;
                    }
                }

                var intersection = (vector * t) + from;
                if (intersection.X >= m_globalMinX && intersection.X <= m_globalMaxX 
                    && intersection.Y >= m_globalMinY && intersection.Y <= m_globalMaxY
                    && intersection.Z >= m_globalMinZ && intersection.Z <= m_globalMaxZ)
                {
                    if (t > currentMinDistanceProxy)
                    {
                        hitBeyond = true;
                        if (hitBehind)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        // hit found that is within currentMinDistanceProxy
                        return true;
                    }
                }
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsPointOnPolygon(Point3D intersection, int index)
    {
        // If the point lies outside of the min-max ranges then it can't be on the face
        var min = m_cachedMins[index];
        var max = m_cachedMaxes[index];
        if (intersection.X >= min.X && intersection.X <= max.X 
            && intersection.Y >= min.Y && intersection.Y <= max.Y
            && intersection.Z >= min.Z && intersection.Z <= max.Z)
        {
            // Now we need to check to see if it is within the polygon
            int drop = m_cachedDrops[index];
            double t0 = drop == 0 ? intersection.Y : intersection.X;
            double t1 = drop == 2 ? intersection.Y : intersection.Z;

            if (PointInPolygon.Check(m_cachedVertex0s[index], m_cachedVertex1s[index], t0, t1))
            {
                return true;
            }
        }

        return false;
    }

    public readonly Point3D[] EdgePoints;

    public readonly Point3D[] CachedNormals;

    private readonly Point3D[] m_points;
    private readonly int[][] m_faces;

    private readonly Point3D[] m_cachedMins;
    private readonly Point3D[] m_cachedMaxes;
    private readonly int[] m_cachedDrops;
    private readonly double[][] m_cachedVertex0s;
    private readonly double[][] m_cachedVertex1s;
    private double m_globalMinX;
    private double m_globalMinY;
    private double m_globalMinZ;
    private double m_globalMaxX;
    private double m_globalMaxY;
    private double m_globalMaxZ;
}