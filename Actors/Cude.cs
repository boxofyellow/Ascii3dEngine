using System;
using System.Collections.Generic;

namespace Ascii3dEngine
{
    public class Cube : Actor
    {
        public Cube(Settings settings, CharMap map) : base()
        {
            m_spin = settings.Spin;
            m_hideBack = settings.HideBack;
            m_map = map;

            for (int i = default; i < m_points.Length; i++)
            {
                m_points[i] = m_points[i] * (c_size / 2.0);
            }

            m_idsRangeStart = ReserveIds(m_points.Length);
        }

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

        public override void Render(Projection projection, bool[,] imageData, List<Label> lables)
        {
            foreach ((char l, int i1, int i2, int i3, int i4) in m_faces)
            {
                Point3D p1 = m_points[i1];
                Point3D p2 = m_points[i2];
                Point3D p3 = m_points[i3];
                Point3D p4 = m_points[i4];
                Point3D average = new Point3D(
                    (p1.X + p2.X + p3.X + p4.X)/4.0,
                    (p1.Y + p2.Y + p3.Y + p4.Y)/4.0,
                    (p1.Z + p2.Z + p3.Z + p4.Z)/4.0);

                if (m_hideBack)
                {
                    Point3D normal = (p1 - average).CrossProduct(p2 - average).Normalized();
                    // when the dot product is > 0 it is a "back plane" (pointing away from the camera)
                    if ((average - projection.Camera.From).DotProduct(normal) > 0.0)
                    {
                        continue;
                    }
                }

                (bool inView, _, Point2D projectedP2) = projection.Trans_Line(new Point3D(), average);
                if (inView)
                {
                    lables.Add(new Label(
                        projectedP2.H / m_map.MaxX,
                        projectedP2.V / m_map.MaxY,
                        l));
                }

                imageData.DrawLine(projection, p1, p2);
                imageData.DrawLine(projection, p2, p3);
                imageData.DrawLine(projection, p3, p4);
                imageData.DrawLine(projection, p4, p1);
            }
        }

        public override void StartRayRender()
        {
            m_equations ??= new (Point3D Normal, double D)[m_faces.Length];

            int index = 0;
            foreach ((_, int i1, int i2, int i3, _) in m_faces)
            {
                Point3D p1 = m_points[i1];
                Point3D p2 = m_points[i2];
                Point3D p3 = m_points[i3];

                Point3D v1 = p2 - p1;
                Point3D v2 = p3 - p1;
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
            foreach ((char l, int i1, int i2, int i3, int i4) in m_faces)
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

                            int[] indexes = new int[] {i1, i2, i3, i4};

                            //
                            // This looks like more stuff to cache...
                            double minX = double.MaxValue, minY = minX, minZ = minX;
                            double maxX = double.MinValue, maxY = maxX, maxZ = maxX;
                            for (int i = 0; i < indexes.Length; i++)
                            {
                                Point3D p = m_points[indexes[i]];
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
                                double[] v0 = new double[indexes.Length];
                                double[] v1 = new double[indexes.Length];
                                for (int i = 0; i < indexes.Length; i++)
                                {
                                    Point3D p = m_points[indexes[i]];
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

        private readonly Point3D[] m_points = new []
        {
            new Point3D(-1, 1, 1),   // font upper left
            new Point3D(1, 1, 1),    // font upper right
            new Point3D(-1, -1, 1),  // font lower left
            new Point3D(1, -1, 1),   // font lower right

            new Point3D(-1, 1, -1),  // back upper left
            new Point3D(1, 1, -1),   // back upper right
            new Point3D(-1, -1, -1), // back lower left
            new Point3D(1, -1, -1),  // back lower right
        };

        private readonly (char Label, int I1, int I2, int I3, int I4)[] m_faces = new []
        {
           ('F', c_frontUpperRight, c_frontUpperLeft, c_frontLowerLeft, c_frontLowerRight), // Front
           ('B', c_backUpperLeft, c_backUpperRight, c_backLowerRight, c_backLowerLeft),     // Back
           ('R', c_backUpperRight, c_frontUpperRight, c_frontLowerRight, c_backLowerRight), // Right
           ('L', c_frontUpperLeft, c_backUpperLeft, c_backLowerLeft, c_frontLowerLeft),     // Left
           ('t', c_frontUpperLeft, c_frontUpperRight, c_backUpperRight, c_backUpperLeft),   // top
           ('b', c_frontLowerRight, c_frontLowerLeft, c_backLowerLeft, c_backLowerRight),   // bottom
        };

        // These will be stored in the form 
        // Normal.X * x + Normal.Y * y + Normal.Z * z + D = 0
        private (Point3D Normal, double D)[] m_equations;

        private const int c_frontUpperLeft =  0;
        private const int c_frontUpperRight =  1;
        private const int c_frontLowerLeft =  2;
        private const int c_frontLowerRight =  3;
        private const int c_backUpperLeft =  4;
        private const int c_backUpperRight =  5;
        private const int c_backLowerLeft =  6;
        private const int c_backLowerRight =  7;

        private readonly bool m_spin;
        private readonly bool m_hideBack;
        private readonly CharMap m_map;
        private readonly int m_idsRangeStart;

        private const double c_size = 25;
    }
}