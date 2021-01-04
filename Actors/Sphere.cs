using System;
using System.Collections.Generic;

namespace Ascii3dEngine
{
    public class Sphere : Actor
    {
        public Sphere(Point3D center, double radius, ColorProperties properties)
        {
            m_id = ReserveIds(1);
            Center = center;
            m_radius = radius;
            m_properties = properties;

            m_rSquared = m_radius * m_radius;
        }

        public override void Render(Projection projection, bool[,] imageData, List<Label> labels)
        {
            // Ok, maybe we should di this later
            throw new NotImplementedException();
        }

        public override ColorProperties ColorAt(Point3D intersection, int id) => m_properties;

        // Gosh Spheres are easy :) if the sphere was centered at the origin the normal is just the intersection point.
        public override Point3D NormalAt(Point3D intersection, int id) => intersection - Center;

        public override (double DistranceProxy, int Id, Point3D Intersection) RenderRay(Point3D from, Point3D vector, double currentMinDistanceProxy)
        {
            (bool hit, double t) = FindIntersection(from, vector);
            if (hit)
            {
                return (t, m_id, from + (vector * t));
            }
            return default;
        }

        public override bool DoesItCastShadow(int sourceIndex, Point3D from, Point3D vector, int minId)
        {
            if (minId != m_id)
            {
                (bool hit, double t) = FindIntersection(from, vector);
                // We will cast a shadow iff and only if we hit and with hit within the length of our vector
                return hit && t < 1.0;
            }
            return false;
        }

        // Checking if an ray hits a sphere...
        // https://en.wikipedia.org/wiki/Line%E2%80%93sphere_intersection && https://math.stackexchange.com/questions/1939423/calculate-if-vector-intersects-sphere
        // Our sphere is defined as ‖𝐗−𝐂‖²=𝑟².
        // Our ray 𝐋(𝑡)=𝐏+𝑡𝐔  (And we We are looking for 𝐗 = 𝐋(𝑡))
        // (𝐗−𝐂)⋅(𝐗−𝐂)=𝑟²
        // (𝐏+𝑡𝐔−𝐂)⋅(𝐏+𝑡𝐔−𝐂)=𝑟²
        // (𝐏−𝐂)⋅(𝐏−𝐂)−𝑟²+2𝑡𝐔⋅(𝐏−𝐂)+𝑡²(𝐔⋅𝐔)=0
        private (bool Hit, double T) FindIntersection(Point3D p, Point3D u)
        {
/*
// C = center of sphere
// r = radius of sphere
// P = point on line  (our From point)
// U = unit vector in direction of line

Q = P - C;
a = U*U;      // should be = 1 (It could be, but I think I just use our ray vector, but it will always be positive)
b = 2*U*Q
c = Q*Q - r*r;
d = b*b - 4*a*c;  // discriminant of quadratic

if d <  0 then solutions are complex, so no intersections
if d >= 0 then solutions are real, so there are intersections

// To find intersections (if you want them)
(t1,t2) = QuadraticSolve(a, b, c);
   if t1 >= 0 then P1 = P + t1*U;   // first intersection
   if t2 >= 0 then P2 = P + t2*U;   // second intersection
*/
            Point3D q = p - Center;
            double a = u.DotProduct(u);
            double b = 2 * u.DotProduct(q);
            double c = q.DotProduct(q) - m_rSquared;
            double d = b * b - (4 * a * c);

            if (d < 0)
            {
                return default;
            }

            // https://en.wikipedia.org/wiki/Quadratic_equation
            // t = (-b +/- √(b² - 4ac)) / 2a

            double sqr = Math.Sqrt(d);
            b *= -1;
            
            // We want the first one, that is in fount.
            if (b > sqr)
            {
                // The only way netave version can help is if the b is negative (and larget then our square root)
                // if we get this far, then this one must be the closest one 
                return (true, (b - sqr) / (2 * a));
            }

            // if that did not work, may the + version will do the trick for us
            double t = (b + sqr);
            if (t > 0)
            {
                return (true, t / (2 * a));
            }

            return default;
        }

        protected Point3D Center;

        private readonly int m_id;
        private double m_radius;
        private double m_rSquared;
        private ColorProperties m_properties;
    }
}