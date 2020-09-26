
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

// http://www.codeincodeblock.com/2012/03/projecting-3d-world-co-ordinates-into.html

namespace Ascii3dEngine
{
    public struct Line3D
    {
        public Line3D(Point3D start, Point3D end)
        {
            Start = start;
            End = end;
        }
        public readonly Point3D Start;
        public readonly Point3D End;
    }

    public struct  Label
    {
        public Label(int column, int row, char character)
        {
            Column = column;
            Row = row;
            Character = character;
        }

        public readonly int Column;
        public readonly int Row;
        public readonly char Character;
    }

    public class Actor
    {
        public Actor(Point3D origin = default) => Origin = origin ?? new Point3D();

        public Point3D Origin { get; set; }
        public IEnumerable<(Point3D Start, Point3D End)> RenderLines => AllLines.Select(x => (Origin + x.Start, Origin + x.End));
        protected readonly List<Line3D> AllLines = new List<Line3D>();
        public virtual void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime, Camera camera) {}
        public virtual void Render(Projection projection, bool[,] imageData, List<Label> lables)
        {
            foreach (Line3D line in AllLines)
            {
                (bool inView, Point2D p1, Point2D p2) = projection.Trans_Line(Origin + line.Start, Origin + line.End);
                if (inView)
                {
                    imageData.DrawLine(p1, p2);
                }
            }
        }
    }

    public class Scene
    {

        public Scene(Settings settings, Point2D size)
        {
            Screen = new Screen(size);
            Camera = new Camera(settings);
        }

        public readonly Camera Camera;
        public readonly Screen Screen;

        public void AddActor(Actor actor) => m_actors.Add(actor);

        public void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime)
        {
            foreach (Actor actor in m_actors)
            {
                actor.Act(timeDelta, elapsedRuntime, Camera);
            }
        }

        public (bool[,] ImageData, List<Label> Labels) Render()
        {
            Projection projection = new Projection(Camera, Screen);
            bool[,] imageData = new bool[(int)Screen.Size.H, (int)Screen.Size.V];
            List<Label> labels = new List<Label>();
            foreach (Actor actor in m_actors)
            {
                actor.Render(projection, imageData, labels);
            }
            return (imageData, labels);
        }

        private List<Actor> m_actors = new List<Actor>();
    }

    public class Point3D
    {
        public double X, Y, Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point3D() : this(0.0, 0.0, 0.0) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point3D(Point3D toClone) : this(toClone.X, toClone.Y, toClone.Z) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point3D(double x, double y, double z)
        {
            X = x; Y = y; Z = z;
        }

        public static Point3D Parse(string value, Point3D defaultValue = null)
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue ?? new Point3D();
            }

            string temp = value.TrimStart('{').TrimEnd('}');
            string[] pieces = temp.Split(",");
            try
            {
                return new Point3D(
                    double.Parse(pieces[0].Trim()),
                    double.Parse(pieces[1].Trim()),
                    double.Parse(pieces[2].Trim())
                );
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Failed to parse {value} {ex}");
                throw;
            }
        }

        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z); 

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize()
        {
            double length = Length;
            X /= length;
            Y /= length;
            Z /= length;
        }

        public Point3D Rotate(Point3D angle)
        {
            double x = X;
            double y = Y;
            double z = Z;
            if (angle.X != 0.0)
            {
                (y, z) = Utilities.Rotate(y, z, angle.X);
            }
            if (angle.Y != 0.0)
            {
                (x, z) = Utilities.Rotate(x, z, angle.Y);
            }
            if (angle.Z != 0.0)
            {
                (x, y) = Utilities.Rotate(x, y, angle.Z);
            }
            return new Point3D(x, y, z);
        }

        public bool IsZero => X == 0.0 && Y == 0.0 && Z == 0.0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point3D CrossProduct(Point3D vector) => new Point3D(
                (Y * vector.Z) - (Z * vector.Y),
                (Z * vector.X) - (X * vector.Z),
                (X * vector.Y) - (Y * vector.X));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double DotProduct(Point3D other)
            => (X * other.X) + (Y * other.Y) + (Z * other.Z);

        public Point3D ApplyAffineTransformation(double[,] transformation)
        {
            if (transformation == null || transformation.GetLength(0) != 4 || transformation.GetLength(1) != 4)
            {
                throw new ArgumentException($"Not correct size ({transformation?.GetLength(0)}, {transformation?.GetLength(1)})", nameof(transformation));
            }

            // Page 216
            // Using row follow by column here
            return new Point3D(
                X * transformation[0,0] + Y * transformation[0, 1] + Z * transformation[0, 2] + transformation[0, 3],
                X * transformation[1,0] + Y * transformation[1, 1] + Z * transformation[1, 2] + transformation[1, 3],
                X * transformation[2,0] + Y * transformation[2, 1] + Z * transformation[2, 2] + transformation[2, 3]
            );
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point3D operator +(Point3D a, Point3D b) => new Point3D(
                a.X + b.X,
                a.Y + b.Y,
                a.Z + b.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point3D operator -(Point3D a, Point3D b) => new Point3D(
                a.X - b.X,
                a.Y - b.Y,
                a.Z - b.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point3D operator *(Point3D a, double n) => new Point3D(
                a.X*n,
                a.Y*n,
                a.Z*n);

        public override string ToString() => $"{{{X}, {Y}, {Z}}}";
    }

    public struct Point2D
    {
        public readonly int H, V;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point2D(int h, int v)
        {
            H = h; V = v;
        }

        public override string ToString() => $"{{{H}, {V}}}";
    }

    public class Camera
    {
        public Point3D From;                 // where the Camera is

        public Point3D To;                   // a point that the Camera is pointing at
                                             // We don't normalize this one like To, should we?
        public Point3D Up                    // a point representing the top of the Camera
        {
            get => m_up;
            set 
            {
                Point3D direction = Direction;
                m_up = new Point3D(value);
                if (direction.IsZero)
                {
                    m_up.Normalize();
                }
                else
                {
                    AlineUp(direction);
                }
            }
        }

        public double HorizontalAngle;       // the angel that can be viewed accross the horizon
        public double VerticalAngle;         // the angel that can be viewed from top to bottom.
        public double Zoom;                  // How zoomed in we are
        public double FrontClippingDistance; // Objects that less then this distance from the camera can't be seen
        public double BackClippingDistance;  // Objects that are farther than this distance from the camera can't be seen 
        public int MovementSpeed;
        public Camera(Settings setting)
        {
            m_settings = setting;
            ResetPosition();
            HorizontalAngle = 45.0;
            VerticalAngle = 45.0;
            Zoom = 1.0;
            FrontClippingDistance = 1.0;
            BackClippingDistance = 200.0;
            MovementSpeed = 1;
        }

        public void ResetPosition()
        {
            From = m_settings.GetFrom();
            To = m_settings.GetTo();
            Up = m_settings.GetUp();
        }

        public void MoveForward()
        {
            Point3D direction = Direction;
            direction.Normalize();
            Move(direction * MovementSpeed);
        }

        public void MoveBackward()
        {
            Point3D direction = Direction;
            direction.Normalize();
            Move(direction * -MovementSpeed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TurnLeft() => Look(Up, 1); // We Don't need to Aline Up b/c we are rotating around Up

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TurnRight() => Look(Up, -1); // We Don't need to Aline Up b/c we are rotating around Up

        // It looks like this would also get the job done.. => To = From - Direction;
        public void AboutFace() => Look(Up, 180);  // turn around 180 degres

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TurnUp()
        {
            Look(Side, -1);
            AlineUp();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TurnDown()
        {
            Look(Side, 1);
            AlineUp();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveUp() => Move(Up * MovementSpeed);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveDown() => Move(Up * -MovementSpeed);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveLeft() => Move(Side * -MovementSpeed);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveRight() => Move(Side * MovementSpeed);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SpinClockwise() => Spin(-1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SpinCounterClockwise() => Spin(1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Move(Point3D delta)
        {
            From = From + delta;
            To = To + delta;
            // We Don't need to Aline Up b/c we are not changing Direction
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Spin(double angle)
        {
            Point3D direction = Direction;
            direction.Normalize();
            Up = Up.ApplyAffineTransformation(Utilities.AffineTransformationForRotatingAroundUnit(direction, angle * Math.PI / 180.0));
            // This is chaning Up, so it will get "Alined"
        }

        private Point3D Direction => To - From;

        private Point3D Side
        {
            get 
            {
                Point3D result = Direction.CrossProduct(Up);
                result.Normalize();
                return result;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Look(Point3D around, double angle) 
            => To = Direction.ApplyAffineTransformation(Utilities.AffineTransformationForRotatingAroundUnit(around, angle * Math.PI / 180.0, From));

        //adjust it so that it is prepandicular to To-From
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AlineUp(Point3D direction = null)
        {
            direction ??= Direction;
            m_up = m_up.CrossProduct(direction).CrossProduct(new Point3D() - direction);
            m_up.Normalize();
        }

        private Point3D m_up;

        private readonly Settings m_settings;
    }

    public class Screen
    {
        public Point2D Center;
        public Point2D Size;
        public Screen(Point2D size)
        {
            Size = size;
            Center = new Point2D(Size.H / 2, Size.V / 2);
        }
    }

    public class Projection
    {
        public Screen Screen;
        private readonly static Point3D s_origin = new Point3D();
        private Point3D m_n1, m_n2;
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

            m_n1 = new Point3D();
            m_n2 = new Point3D();

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

           m_basisB = Camera.To - Camera.From;
           m_basisB.Normalize();

           m_basisA = Camera.Up.CrossProduct(m_basisB);
           m_basisA.Normalize();
 
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

        private bool Trans_ClipEye(Point3D e1, Point3D e2)
        {
            double mu;

            /* Is the vector totally in front of the front cutting plane ? */
            if (e1.Y <= Camera.FrontClippingDistance && e2.Y <= Camera.FrontClippingDistance)
            {
                return false;
            }

            /* Is the vector totally behind the back cutting plane ? */
            if (e1.Y >= Camera.BackClippingDistance && e2.Y >= Camera.BackClippingDistance)
            {
                return false;
            }

            /* Is the vector partly in front of the front cutting plane ? */
            if ((e1.Y < Camera.FrontClippingDistance && e2.Y > Camera.FrontClippingDistance) ||
               (e1.Y > Camera.FrontClippingDistance && e2.Y < Camera.FrontClippingDistance))
            {
                mu = (Camera.FrontClippingDistance - e1.Y) / (e2.Y - e1.Y);
                if (e1.Y < Camera.FrontClippingDistance)
                {
                    e1.X = e1.X + mu * (e2.X - e1.X);
                    e1.Z = e1.Z + mu * (e2.Z - e1.Z);
                    e1.Y = Camera.FrontClippingDistance;
                }
                else
                {
                    e2.X = e1.X + mu * (e2.X - e1.X);
                    e2.Z = e1.Z + mu * (e2.Z - e1.Z);
                    e2.Y = Camera.FrontClippingDistance;
                }
            }
            /* Is the vector partly behind the back cutting plane ? */
            if ((e1.Y < Camera.BackClippingDistance && e2.Y > Camera.BackClippingDistance) ||
               (e1.Y > Camera.BackClippingDistance && e2.Y < Camera.BackClippingDistance))
            {
                mu = (Camera.BackClippingDistance - e1.Y) / (e2.Y - e1.Y);
                if (e1.Y < Camera.BackClippingDistance)
                {
                    e2.X = e1.X + mu * (e2.X - e1.X);
                    e2.Z = e1.Z + mu * (e2.Z - e1.Z);
                    e2.Y = Camera.BackClippingDistance;
                }
                else
                {
                    e1.X = e1.X + mu * (e2.X - e1.X);
                    e1.Z = e1.Z + mu * (e2.Z - e1.Z);
                    e1.Y = Camera.BackClippingDistance;
                }
            }
            return true;
        }

        private Point3D Trans_Eye2Norm(Point3D e)
        {
            double d = Camera.Zoom / e.Y;
            return new Point3D(d * e.X / m_tanthetah,
                               e.Y,
                               d * e.Z / m_tanthetav);
        }

        private bool Trans_ClipNorm(Point3D n1, Point3D n2)
        {
            double mu;

            /* Is the line segment totally right of x = 1 ? */
            if (n1.X >= 1 && n2.X >= 1)
            {
                return false;
            }


            /* Is the line segment totally left of x = -1 ? */
            if (n1.X <= -1 && n2.X <= -1)
            {
                return false;
            }

            /* Does the vector cross x = 1 ? */
            if ((n1.X > 1 && n2.X < 1) || (n1.X < 1 && n2.X > 1))
            {
                mu = (1 - n1.X) / (n2.X - n1.X);
                if (n1.X < 1)
                {
                    n2.Z = n1.Z + mu * (n2.Z - n1.Z);
                    n2.X = 1;
                }
                else
                {
                    n1.Z = n1.Z + mu * (n2.Z - n1.Z);
                    n1.X = 1;
                }
            }

            /* Does the vector cross x = -1 ? */
            if ((n1.X < -1 && n2.X > -1) || (n1.X > -1 && n2.X < -1))
            {
                mu = (-1 - n1.X) / (n2.X - n1.X);
                if (n1.X > -1)
                {
                    n2.Z = n1.Z + mu * (n2.Z - n1.Z);
                    n2.X = -1;
                }
                else
                {
                    n1.Z = n1.Z + mu * (n2.Z - n1.Z);
                    n1.X = -1;
                }
            }

            /* Is the line segment totally above z = 1 ? */
            if (n1.Z >= 1 && n2.Z >= 1)
            {
                return false;
            }

            /* Is the line segment totally below z = -1 ? */
            if (n1.Z <= -1 && n2.Z <= -1)
            {
                return false;
            }

            /* Does the vector cross z = 1 ? */
            if ((n1.Z > 1 && n2.Z < 1) || (n1.Z < 1 && n2.Z > 1))
            {

                mu = (1 - n1.Z) / (n2.Z - n1.Z);
                if (n1.Z < 1)
                {
                    n2.X = n1.X + mu * (n2.X - n1.X);
                    n2.Z = 1;
                }
                else
                {
                    n1.X = n1.X + mu * (n2.X - n1.X);
                    n1.Z = 1;
                }
            }

            /* Does the vector cross z = -1 ? */
            if ((n1.Z < -1 && n2.Z > -1) || (n1.Z > -1 && n2.Z < -1))
            {

                mu = (-1 - n1.Z) / (n2.Z - n1.Z);
                if (n1.Z > -1)
                {
                    n2.X = n1.X + mu * (n2.X - n1.X);
                    n2.Z = -1;
                }
                else
                {
                    n1.X = n1.X + mu * (n2.X - n1.X);
                    n1.Z = -1;
                }

            }
            return true;
        }

        private Point2D Trans_Norm2Screen(Point3D norm)  => new Point2D(
            Convert.ToInt32(Screen.Center.H - Screen.Size.H * norm.X / 2),
            Convert.ToInt32(Screen.Center.V - Screen.Size.V * norm.Z / 2)
        );

        public (bool InView, Point2D P1, Point2D P2) Trans_Line(Point3D w1, Point3D w2)
        {
            Point3D e1 = Trans_World2Eye(w1);
            Point3D e2 = Trans_World2Eye(w2);
            if (Trans_ClipEye(e1, e2))
            {
                m_n1 = Trans_Eye2Norm(e1);
                m_n2 = Trans_Eye2Norm(e2);
                if (Trans_ClipNorm(m_n1, m_n2))
                {
                    return (true,
                            Trans_Norm2Screen(m_n1),
                            Trans_Norm2Screen(m_n2));
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