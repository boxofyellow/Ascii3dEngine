using System;
using System.Runtime.CompilerServices;

namespace Ascii3dEngine
{
    /// <summary>
    /// This is used to cache data that is dependent on the origin of our ray
    /// </summary>
    public sealed class PolygonActorOriginDependentCache
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PolygonActorOriginDependentCache(Point3D[] points, int[][] faces)
        {
            m_points = points;
            m_faces = faces;
            m_cachedNumerators = new double[m_faces.Length];
            EdgeCachedNumerators = new double[CubeDefinition.Faces.Length];
        }

        public void Update(Point3D from, bool dirty, PolygonActorOriginIndependentCache independentCache)
        {
            if (!dirty && m_lastFrom == from)
            {
                return;
            }

            int index = 0;
            foreach (int[] pointIndexes in m_faces)
            {
                if (pointIndexes.Length < 3)
                {
                    throw new Exception("Can't draw single points/lines, maybe we should, feel free to add code here later when needed");
                }

                Point3D p1 = m_points[pointIndexes[0]];
                Point3D normal = independentCache.CachedNormals[index];

                // From here we know
                // https://www.youtube.com/watch?v=0qYJfKG-3l8
                // normal.X * (x - p1.X) + normal.Y * (y - p1.Y) + normal.Z * (z - p1.Z) = 0
                // So to get into the form that we want, just multiple (and collect) all the invariant terms to Get D. A, B and C are just normal.X, normal.Y and normal.Z respectively 
                //
                // This video describes finding the point where a line intersects a plane
                // https://www.youtube.com/watch?v=qVvvy5hsQwk
                // And we already have our planes equation
                // So far we have
                // normal.X * x + normal.Y * y + normal.Z * y + D = 0
                // The ray provided later will be our vector (that is going to change as check each ray).
                // from will be the view point's origin, that will remain constant as we check each ray
                // This gives us
                // r(t) = vector * t + from = <t * vector.X + from.X, t * vector.Y + from.Y, t * vector.Z + from.Z>
                // and from above
                // normal.X * (t * vector.X + from.X) + normal.Y * (t * vector.Y + from.Y) + normal.Z * (t * vector.Z + from.Z) + D = 0
                // t * ((normal.X * vector.X) + (normal.Y * vector.Y) + (normal.Z * vector.Z)) + (normal.X * from.X) + (normal.Y * from.Y) + (normal.Z * from.Z) + D = 0
                // t = -((normal.X * from.X) + (normal.Y * from.Y) + (normal.Z * from.Z) + D) / ((normal.X * vector.X) + (normal.Y * vector.Y) + (normal.Z * vector.Z))
                // the numerator is made of constant value, so we can compute that now
                double d = (normal.X * -p1.X) + (normal.Y * -p1.Y) + (normal.Z * -p1.Z);
                double numerator = -((normal.X * from.X) + (normal.Y * from.Y) + (normal.Z * from.Z) + d);
                m_cachedNumerators[index] = numerator;

                //
                // One thing to keep in mind as we continue, Our general plan is to keep elimiating things as we go.
                // So we have to mindful of ratio of how often we get to the point where this data would be useful  compared to where we bail early.
                // For example there is the if (t > 0) check in RenderRay, this excludes thing that is behind the camera (so like 1/2 the world)
                // 

                index++;
            }

            for (int i = default; i < CubeDefinition.Faces.Length; i++)
            {
                Point3D p1 = independentCache.EdgePoints[CubeDefinition.Faces[i][0]];
                Point3D normal = CubeDefinition.Normals[i];
                double d = (normal.X * -p1.X) + (normal.Y * -p1.Y) + (normal.Z * -p1.Z);
                EdgeCachedNumerators[i] = -((normal.X * from.X) + (normal.Y * from.Y) + (normal.Z * from.Z) + d);
            }

            m_lastFrom = from;
        }

        public (double DistranceProxy, int Index, Point3D Intersection) FindClosestIntersection(
            Point3D vector, double currentMinDistanceProxy, PolygonActorOriginIndependentCache independentCache)
        {
            int result = -1;
            Point3D minIntersection = default;
            for (int index = default; index < m_faces.Length; index++)
            {
                Point3D normal = independentCache.CachedNormals[index];
                double denominator = (normal.X * vector.X) + (normal.Y * vector.Y) + (normal.Z * vector.Z);
                if (denominator != 0)
                {
                    double numerator = m_cachedNumerators[index];
                    double t = numerator / denominator;
                    if (t > 0)
                    {
                        // when t > 0, that mean we are starting at from, and moving along the direction of the positive vector so we can see this, if t < 0, then the interection point is behind us
                        // we can compute the intersection with vector * t + from, but what we really want is distance
                        // Since we are comparing points along the same vector, we already have what we need, t 

                        if (t < currentMinDistanceProxy)
                        {
                            Point3D intersection = (vector * t) + m_lastFrom;

                            // Of all the faces we have tried thus far, we know the point where the ray intersects this plane is the closest
                            // But we need to make sure that the intersection point is within this face
                            if (independentCache.IsPointOnPolygon(intersection, index))
                            {
                                result = index;
                                minIntersection = intersection;
                                currentMinDistanceProxy = t;
                            }
                        }
                    }
                }
            }
            return (currentMinDistanceProxy, result, minIntersection);
        }

        public bool IsIntersectionWithInOne(Point3D vector, int indexToIgnore, PolygonActorOriginIndependentCache independentCache)
        {
            for (int index = default; index < m_faces.Length; index++)
            {
                if (index != indexToIgnore)
                {
                    Point3D normal = independentCache.CachedNormals[index];
                    double denominator = (normal.X * vector.X) + (normal.Y * vector.Y) + (normal.Z * vector.Z);
                    if (denominator != 0)
                    {
                        double numerator = m_cachedNumerators[index];
                        double t = numerator / denominator;
                        if (t > 0 && t < 1.0)
                        {
                            Point3D intersection = (vector * t) + m_lastFrom;
                            if (independentCache.IsPointOnPolygon(intersection, index))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public readonly double[] EdgeCachedNumerators;

        private readonly int[][] m_faces;
        private readonly Point3D[] m_points;

        private Point3D m_lastFrom;
        private readonly double[] m_cachedNumerators;
    }
}