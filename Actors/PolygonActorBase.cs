using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascii3dEngine
{
    public abstract class PolygonActorBase : Actor
    {
        public PolygonActorBase(Settings settings) : base()
        {
            m_spin = settings.Spin;
            m_hideBack = settings.HideBack;
            (m_points, m_faces) = GetData(settings);
            m_idsRangeStart = ReserveIds(m_faces.Length);
        }

        protected abstract (Point3D[] Points, int[][] Faces) GetData(Settings settings);

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

        public override void StartRayRender()
        {
            m_equations ??= new (Point3D Normal, double D)[m_faces.Length];

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
                // From here we know
                // https://www.youtube.com/watch?v=0qYJfKG-3l8
                // normal.X * (x - p1.X) + normal.Y * (y - p1.Y) + normal.Z * (z - p1.Z) = 0
                // So to get into the form that we want, just multiple all the invariant terms to Get D. A, B and C are just Normal.X, Normal.Y and Normal.Z respectively 
                m_equations[index] = (normal, (normal.X * -p1.X) + (normal.Y *-p1.Y) + (normal.Z *-p1.Z));
                
                index++;
            }
        }

        public override (double Distrance, int Id) RenderRay(Point3D from, Point3D vector)
        {
            int id = 0;
            double minDistance = double.MaxValue;
            
            int index = 0;
            foreach (int[] pointIndexes in m_faces)
            {
                // This video describes finding the point where a line intersects a plane
                // https://www.youtube.com/watch?v=qVvvy5hsQwk
                // We already computed our planes equation and store them in m_equations
                // So far we have
                // Normal.X * x + Normal.Y * y + Normal.Z * y + D = 0
                // r(t) = vector * t + from = <t * vector.X + from.X, t * vector.Y + from.Y, t * vector.Z + from.Z>
                // Normal.X * (t * vector.X + from.X) + Normal.Y * (t * vector.Y + from.Y) + Normal.Z * (t * vector.Z + from.Z) + D = 0
                // t * ((Normal.X * vector.X) + (Normal.Y * vector.Y) + (Normal.Z * vector.Z)) + (Normal.X * from.X) + (Normal.Y * from.Y) + (Normal.Z * from.Z) + D = 0
                // t = -((Normal.X * from.X) + (Normal.Y * from.Y) + (Normal.Z * from.Z) + D) / ((Normal.X * vector.X) + (Normal.Y * vector.Y) + (Normal.Z * vector.Z))
                (Point3D normal, double d) = m_equations[index];
                double denominator = (normal.X * vector.X) + (normal.Y * vector.Y) + (normal.Z * vector.Z);
                if (denominator != 0)
                {
                    // hmmmmm.... this looks rather static, I mean this value should not change for each ray... maybe we should be caching this instead of D
                    double numerator = -((normal.X * from.X) + (normal.Y * from.Y) + (normal.Z * from.Z) + d);
                    double t = numerator / denominator;
                    if (t > 0)
                    {
                        // when t > 0, that mean we are moving starting at from, and moving the directory of vector the point so we can see this, if t < 0, then the interection point is behind us
                        // we can compute the intersection with vector * t + from, but what we really want is distance
                        Point3D ray = vector * t;
                        double distance = ray.Length;
                        if (distance < minDistance)
                        {
                            Point3D intersection = from + ray;

                            // Of all the faces we have tried thus far, we know the point where the ray intersects the this plane is the closest
                            // But we need to make sure that the intersection point is within the face
                            // We have been assuming that all the points that make up this face are in a plane, and we know the intersection point is in the plane, we can now drop one of the dementions
                            // We want to drop the demention with the least variation.

                            //
                            // This looks like more stuff to cache...
                            double minX = double.MaxValue, minY = minX, minZ = minX;
                            double maxX = double.MinValue, maxY = maxX, maxZ = maxX;
                            for (int i = 0; i < pointIndexes.Length; i++)
                            {
                                Point3D p = m_points[pointIndexes[i]];
                                minX = Math.Min(minX, p.X);
                                minY = Math.Min(minY, p.Y);
                                minZ = Math.Min(minZ, p.Z);

                                maxX = Math.Max(maxX, p.X);
                                maxY = Math.Max(maxY, p.Y);
                                maxZ = Math.Max(maxZ, p.Z);
                            }

                            // If the point lies outside of the min-max ranges then it can't be on the face
                            if (intersection.X >= minX && intersection.X <= maxX && intersection.Y >= minY && intersection.Y <= maxY && intersection.Z >= minZ && intersection.Z <= maxZ)
                            {
                                double rangeX = maxX - minX;
                                double rangeY = maxY - minY;
                                double rangeZ = maxZ - minZ;

                                int drop = rangeX <= rangeY && rangeX <= rangeZ ? 0
                                         : rangeY <= rangeX && rangeY <= rangeZ ? 1
                                         : 2;

                                // This looks a little cryptic... so a table may help make sure we are clear
                                //  drop  | v0 | v1 
                                //  0 - X | Y  | Z
                                //  1 - Y | X  | Z
                                //  2 - Z | X  | Y

                                // if we are dropping X, then use Y
                                double t0 = drop == 0 ? intersection.Y : intersection.X;
                                double t1 = drop == 2 ? intersection.Y : intersection.Z;

                                // the arrays that we are building here also look cache-able
                                double[] v0 = new double[pointIndexes.Length];
                                double[] v1 = new double[pointIndexes.Length];
                                for (int i = 0; i < pointIndexes.Length; i++)
                                {
                                    Point3D p = m_points[pointIndexes[i]];
                                    v0[i] = drop == 0 ? p.Y : p.X;
                                    v1[i] = drop == 2 ? p.Y : p.Z;
                                }

                                if (PointInPolygon.Check(v0, v1, t0, t1))
                                {
                                    id = m_idsRangeStart + index;
                                    minDistance = distance;
                                }
                            }
                        }
                    }
                }

                // We are going to want to use m_equations that we previously computed 
                index++;
            }

            return (minDistance, id);
        }

        // These will be stored in the form 
        // Normal.X * x + Normal.Y * y + Normal.Z * z + D = 0
        private (Point3D Normal, double D)[] m_equations;

        private readonly int[][] m_faces;
        private readonly Point3D[] m_points;

        private readonly bool m_spin;
        private readonly bool m_hideBack;

        private readonly int m_idsRangeStart;
    }
}