using System;
using System.Runtime.CompilerServices;

namespace Ascii3dEngine
{
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
                if (direction.IsZero)
                {
                    m_up = value.Normalized();
                }
                else
                {
                    m_up = value;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveForward() => Move(Direction.Normalized() * MovementSpeed);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveBackward() => Move(Direction.Normalized() * -MovementSpeed);

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
            Point3D direction = Direction.Normalized();
            Up = Up.ApplyAffineTransformation(Utilities.AffineTransformationForRotatingAroundUnit(direction, angle * Math.PI / 180.0));
            // This is chaning Up, so it will get "Alined"
        }

        public Point3D Direction => To - From;

        public Point3D Side => Direction.CrossProduct(Up).Normalized();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Look(Point3D around, double angle) 
            => To = Direction.ApplyAffineTransformation(Utilities.AffineTransformationForRotatingAroundUnit(around, angle * Math.PI / 180.0, From));

        //adjust it so that it is prepandicular to To-From
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AlineUp(Point3D? direction = null)
        {
            Point3D d = direction ??= Direction;
            m_up = m_up.CrossProduct(d).CrossProduct(d * -1).Normalized();
        }

        private Point3D m_up;

        private readonly Settings m_settings;
    }
}