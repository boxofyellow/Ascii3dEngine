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
                    throw new Exception("Can't draw single points, maybe we should, feel free to add code here later when needed");
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
            if (!m_areCachesDirty && m_lastFrom == from)
            {
                return;
            }

            m_cachedNormals ??= new Point3D[m_faces.Length];
            m_cachedNumerators ??= new double[m_faces.Length];
            m_cachedMins ??= new Point3D[m_faces.Length];
            m_cachedMaxes ??= new Point3D[m_faces.Length];
            m_cachedDrops ??= new int[m_faces.Length];
            m_cachedVertex0s ??= new double[m_faces.Length][];
            m_cachedVertex1s ??= new double[m_faces.Length][];

            int index = 0;
            foreach (int[] pointIndexes in m_faces)
            {
                if (pointIndexes.Length < 3)
                {
                    throw new Exception("Can't draw single points, maybe we should, feel free to add code here later when needed");
                }

                Point3D p1 = m_points[pointIndexes[0]];

                Point3D v1 = m_points[pointIndexes[1]] - p1;
                Point3D v2 = m_points[pointIndexes[2]] - p1;
                Point3D normal = v1.CrossProduct(v2);
                m_cachedNormals[index] = normal;
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
                // The above values help us compute where the ray will intersect with the plane that the face is on.
                // That alone is not enough, we need to determine if that intersection point will be within the polygon defined by the face
                // Since we know the intersection point and the face share the same plane, so lets collect more info for this face
                // that we can then use for each of the many-many rays we are going to test

                //
                // Knowing the min/max of each point of the face will help us very coarsely separate point that are clearly not on the face, and those that will require more work 
                // As Pointed out here https://stackoverflow.com/questions/217578/how-can-i-determine-whether-a-2d-point-is-within-a-polygon/16261774#16261774
                // this check can slow things down in the case that most of the points are within the face
                // But that is unlikely to the case.  Plus we can compute this once, and reuse it.
                double minX = p1.X, minY = p1.Y, minZ = p1.Z;
                double maxX = minX, maxY = minY, maxZ = minZ;
                for (int i = 1; i < pointIndexes.Length; i++)
                {
                    Point3D p = m_points[pointIndexes[i]];
                    minX = Math.Min(minX, p.X);
                    minY = Math.Min(minY, p.Y);
                    minZ = Math.Min(minZ, p.Z);

                    maxX = Math.Max(maxX, p.X);
                    maxY = Math.Max(maxY, p.Y);
                    maxZ = Math.Max(maxZ, p.Z);
                }
                m_cachedMins[index] = new Point3D(minX, minY, minZ);
                m_cachedMaxes[index] = new Point3D(maxX, maxY, maxZ);

                //
                // One thing to keep in mind as we continue, Our general plan is to keep elimiating things as we go.
                // So we have to mindful of ratio of how often we get to the point where this data would be useful  compared to where we bail early.
                // For example there is the if (t > 0) check in RenderRay, this excludes thing that is behind the camera (so like 1/2 the world)
                // 

                //
                // We will use pnpoly algorithm to make the final call to tell if the point is in or out.
                // But that algorithm is desired to work in 2D, not 3D space.
                // But we know the all the 3D points being checked are all in the same plane, so we can simply drop one dimensions.
                // We could drop any dimension, but to help avoid rounding shenanigans we should drop the one with smallest range.
                // One could argue that this range calculation should include the yet-to-be-determined intersection point,
                // but for that to make a difference it would have be out side of the min/max, and inturn ignored.
                // And this range-based calculation is one more reason why min/max values come in handy and become worthwhile to compute 
                double rangeX = maxX - minX;
                double rangeY = maxY - minY;
                double rangeZ = maxZ - minZ;

                int drop = rangeX <= rangeY && rangeX <= rangeZ ? 0 // Drop X
                         : rangeY <= rangeX && rangeY <= rangeZ ? 1 // Drop Y
                         : 2;                                       // Drop Z
                m_cachedDrops[index] = drop;

                double[] vertex0s = m_cachedVertex0s[index];
                if (vertex0s == null)
                {
                    vertex0s = new double[pointIndexes.Length];
                    m_cachedVertex0s[index] = vertex0s;
                }

                double[] vertex1s = m_cachedVertex1s[index];
                if (vertex1s == null)
                {
                    vertex1s = new double[pointIndexes.Length];
                    m_cachedVertex1s[index] = vertex1s;
                }

                // This looks a little cryptic... so a table may help make sure we are clear
                //  drop   | v0 | v1 
                //  0 -> X |  Y |  Z
                //  1 -> Y |  X |  Z
                //  2 -> Z |  X |  Y
                for (int i = 0; i < pointIndexes.Length; i++)
                {
                    Point3D p = m_points[pointIndexes[i]];
                    // If we are dropping X, use Y
                    vertex0s[i] = drop == 0 ? p.Y : p.X;
                    // If we are dropping Z, use Y
                    vertex1s[i] = drop == 2 ? p.Y : p.Z;
                }
                index++;
            }

            m_lastFrom = from;
            m_areCachesDirty = false;
        }

        public override (double DistranceProxy, int Id) RenderRay(Point3D from, Point3D vector, double currentMinDistanceProxy)
        {
            int id = default;
            
            int index = default;
            foreach (int[] pointIndexes in m_faces)
            {
                Point3D normal = m_cachedNormals[index];
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
                            Point3D intersection = (vector * t) + from;

                            // Of all the faces we have tried thus far, we know the point where the ray intersects the this plane is the closest
                            // But we need to make sure that the intersection point is within this face

                            // If the point lies outside of the min-max ranges then it can't be on the face
                            Point3D min = m_cachedMins[index];
                            Point3D max = m_cachedMaxes[index];
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
                                    id = GetId(index);
                                    currentMinDistanceProxy = t;
                                }
                            }
                        }
                    }
                }
                index++;
            }

            return (currentMinDistanceProxy, id);
        }

        protected virtual int GetId(int face) => IdsRangeStart + face;

        protected readonly int IdsRangeStart;

        private bool m_areCachesDirty = true;
        private Point3D m_lastFrom;
        private Point3D[] m_cachedNormals;
        private double[] m_cachedNumerators;
        private Point3D[] m_cachedMins;
        private Point3D[] m_cachedMaxes;
        private int[] m_cachedDrops;
        private double[][] m_cachedVertex0s;
        private double[][] m_cachedVertex1s;

        private readonly int[][] m_faces;
        private readonly Point3D[] m_points;

        private readonly bool m_spin;
        private readonly bool m_hideBack;
    }
}