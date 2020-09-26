using System;
using System.Runtime.CompilerServices;

// http://www.codeincodeblock.com/2012/03/projecting-3d-world-co-ordinates-into.html

namespace Ascii3dEngine
{
    public class Projection
    {
        public Screen Screen;
        private readonly static Point3D s_origin = new Point3D();
        public Camera Camera;
        private double m_tanthetah, m_tanthetav;
        private Point3D m_basisA, m_basisB, m_basisC;
        private const double c_epsilon = 0.001;
        private const double c_dtor = 0.01745329252;

        public Projection(Camera camera, Screen screen)
        {
            Camera = camera;
            Screen = screen;
            m_basisA = new Point3D();
            m_basisB = new Point3D();
            m_basisC = new Point3D();

            if (!Trans_Initialize())
            {
                throw new Exception("Error in initializing variable");
            }
        }

        private bool Trans_Initialize()
        {
           /* Is the camera position and view vector coincident ? */
           if (EqualVertex(Camera.To, Camera.From))
           {
               return false;
           }

           /* Is there a legal camera up vector ? */
           if (EqualVertex(Camera.Up, s_origin))
           {
               return false;
           }

           m_basisB = Camera.Direction.Normalized();

           m_basisA = Camera.Up.CrossProduct(m_basisB).Normalized();
 
           /* Are the up vector and view direction colinear */
           if (EqualVertex(m_basisA, s_origin))
           {
               return false;
           }

           m_basisC = m_basisB.CrossProduct(m_basisA);

           /* Do we have legal camera apertures ? */
           if (Camera.HorizontalAngle < c_epsilon || Camera.VerticalAngle < c_epsilon)
           {
               return false;
           }

           /* Calculate camera aperture statics, note: angles in degrees */
           m_tanthetah = Math.Tan(Camera.HorizontalAngle * c_dtor / 2);
           m_tanthetav = Math.Tan(Camera.VerticalAngle * c_dtor / 2);

           /* Do we have a legal camera zoom ? */
           if (Camera.Zoom < c_epsilon)
           {
               return false;
           }

           /* Are the clipping planes legal ? */
           if (Camera.FrontClippingDistance < 0 || Camera.BackClippingDistance < 0 || Camera.BackClippingDistance <= Camera.FrontClippingDistance)
           {
               return false;
           }
           return true;
        }

        private Point3D Trans_World2Eye(Point3D w)
        {
            /* Translate world so that the camera is at the origin */
            Point3D world = w - Camera.From;

            /* Convert to eye coordinates using basis vectors */
            return new Point3D(
                world.X * m_basisA.X + world.Y * m_basisA.Y + world.Z * m_basisA.Z,
                world.X * m_basisB.X + world.Y * m_basisB.Y + world.Z * m_basisB.Z,
                world.X * m_basisC.X + world.Y * m_basisC.Y + world.Z * m_basisC.Z);
        }

        private (bool InView, Point3D Result1, Point3D Result2) Trans_ClipEye(Point3D e1, Point3D e2)
        {
            double mu;

            /* Is the vector totally in front of the front cutting plane ? */
            if (e1.Y <= Camera.FrontClippingDistance && e2.Y <= Camera.FrontClippingDistance)
            {
                return (false, default, default);
            }

            /* Is the vector totally behind the back cutting plane ? */
            if (e1.Y >= Camera.BackClippingDistance && e2.Y >= Camera.BackClippingDistance)
            {
                return (false, default, default);
            }

            /* Is the vector partly in front of the front cutting plane ? */
            if ((e1.Y < Camera.FrontClippingDistance && e2.Y > Camera.FrontClippingDistance) ||
               (e1.Y > Camera.FrontClippingDistance && e2.Y < Camera.FrontClippingDistance))
            {
                mu = (Camera.FrontClippingDistance - e1.Y) / (e2.Y - e1.Y);
                if (e1.Y < Camera.FrontClippingDistance)
                {
                    e1 = new Point3D(
                        e1.X + mu * (e2.X - e1.X),
                        Camera.FrontClippingDistance,
                        e1.Z + mu * (e2.Z - e1.Z));
                }
                else
                {
                    e2 = new Point3D(
                        e1.X + mu * (e2.X - e1.X),
                        Camera.FrontClippingDistance,
                        e1.Z + mu * (e2.Z - e1.Z));
                }
            }
            /* Is the vector partly behind the back cutting plane ? */
            if ((e1.Y < Camera.BackClippingDistance && e2.Y > Camera.BackClippingDistance) ||
               (e1.Y > Camera.BackClippingDistance && e2.Y < Camera.BackClippingDistance))
            {
                mu = (Camera.BackClippingDistance - e1.Y) / (e2.Y - e1.Y);
                if (e1.Y < Camera.BackClippingDistance)
                {
                    e2 = new Point3D(
                        e1.X + mu * (e2.X - e1.X),
                        Camera.BackClippingDistance,
                        e1.Z + mu * (e2.Z - e1.Z));
                }
                else
                {
                    e1 = new Point3D(
                        e1.X + mu * (e2.X - e1.X),
                        Camera.BackClippingDistance,
                        e1.Z + mu * (e2.Z - e1.Z));
                }
            }
            return (true, e1, e2);
        }

        private Point3D Trans_Eye2Norm(Point3D e)
        {
            double d = Camera.Zoom / e.Y;
            return new Point3D(d * e.X / m_tanthetah,
                               e.Y,
                               d * e.Z / m_tanthetav);
        }

        private (bool InView, Point3D Result1, Point3D Result2) Trans_ClipNorm(Point3D n1, Point3D n2)
        {
            double mu;

            /* Is the line segment totally right of x = 1 ? */
            if (n1.X >= 1 && n2.X >= 1)
            {
                return (false, default, default);
            }

            /* Is the line segment totally left of x = -1 ? */
            if (n1.X <= -1 && n2.X <= -1)
            {
                return (false, default, default);
            }

            /* Does the vector cross x = 1 ? */
            if ((n1.X > 1 && n2.X < 1) || (n1.X < 1 && n2.X > 1))
            {
                mu = (1 - n1.X) / (n2.X - n1.X);
                if (n1.X < 1)
                {
                    n2 = new Point3D(
                        1,
                        n2.Y,
                        n1.Z + mu * (n2.Z - n1.Z));
                }
                else
                {
                    n1 = new Point3D(
                        1,
                        n1.Y,
                        n1.Z + mu * (n2.Z - n1.Z));
                }
            }

            /* Does the vector cross x = -1 ? */
            if ((n1.X < -1 && n2.X > -1) || (n1.X > -1 && n2.X < -1))
            {
                mu = (-1 - n1.X) / (n2.X - n1.X);
                if (n1.X > -1)
                {
                    n2 = new Point3D(
                        -1,
                        n2.Y,
                        n1.Z + mu * (n2.Z - n1.Z));
                }
                else
                {
                    n1 = new Point3D(
                        -1,
                        n1.Y,
                        n1.Z + mu * (n2.Z - n1.Z));
                }
            }

            /* Is the line segment totally above z = 1 ? */
            if (n1.Z >= 1 && n2.Z >= 1)
            {
                return (false, default, default);
            }

            /* Is the line segment totally below z = -1 ? */
            if (n1.Z <= -1 && n2.Z <= -1)
            {
                return (false, default, default);
            }

            /* Does the vector cross z = 1 ? */
            if ((n1.Z > 1 && n2.Z < 1) || (n1.Z < 1 && n2.Z > 1))
            {

                mu = (1 - n1.Z) / (n2.Z - n1.Z);
                if (n1.Z < 1)
                {
                    n2 = new Point3D(
                        n1.X + mu * (n2.X - n1.X),
                        n2.Y,
                        1);
                }
                else
                {
                    n1 = new Point3D(
                        n1.X + mu * (n2.X - n1.X),
                        n1.Y,
                        1);
                }
            }

            /* Does the vector cross z = -1 ? */
            if ((n1.Z < -1 && n2.Z > -1) || (n1.Z > -1 && n2.Z < -1))
            {

                mu = (-1 - n1.Z) / (n2.Z - n1.Z);
                if (n1.Z > -1)
                {
                    n2 = new Point3D(
                        n1.X + mu * (n2.X - n1.X),
                        n2.Y,
                        -1);
                }
                else
                {
                    n1 = new Point3D(
                        n1.X + mu * (n2.X - n1.X),
                        n1.Y,
                        -1);
                }

            }
            return (true, n1, n2);
        }

        private Point2D Trans_Norm2Screen(Point3D norm)  => new Point2D(
            Convert.ToInt32(Screen.Center.H - Screen.Size.H * norm.X / 2),
            Convert.ToInt32(Screen.Center.V - Screen.Size.V * norm.Z / 2)
        );

        public (bool InView, Point2D P1, Point2D P2) Trans_Line(Point3D w1, Point3D w2)
        {
            (bool inView, Point3D e1, Point3D e2) = Trans_ClipEye(
                Trans_World2Eye(w1),
                Trans_World2Eye(w2));

            if (inView)
            {
                Point3D n1 = Trans_Eye2Norm(e1);
                Point3D n2 = Trans_Eye2Norm(e2);
                (inView, n1, n2) = Trans_ClipNorm(n1, n2);

                if (inView)
                {
                    return (true,
                            Trans_Norm2Screen(n1),
                            Trans_Norm2Screen(n2));
                }

            }
            return (false, default, default);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool EqualVertex(Point3D p1, Point3D p2) 
            => ((Math.Abs(p1.X - p2.X) <= c_epsilon) 
            && (Math.Abs(p1.Y - p2.Y) <= c_epsilon) 
            && (Math.Abs(p1.Z - p2.Z) <= c_epsilon));
    }
}