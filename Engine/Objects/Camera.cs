using System.Runtime.CompilerServices;

public class Camera
{
    public Point3D From;                 // where the Camera is

    public Point3D To;                   // a point that the Camera is pointing at
                                            // We don't normalize this one like Up, should we?
    public Point3D Up                    // a point representing the top of the Camera
    {
        get => m_up;
        set 
        {
            var direction = Direction;
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

    public int MovementSpeed;
    public Camera(Point3D from, Point3D to, Point3D up)
    {
        ResetPosition(from, to, up);
        MovementSpeed = 1;
    }

    public void ResetPosition(Point3D from, Point3D to, Point3D up)
    {
        From = from;
        To = to;
        Up = up;
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
    public void AboutFace() => Look(Up, 180);  // turn around 180 degrees

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TurnUp()
    {
        Look(Right, -1);
        AlineUp();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TurnDown()
    {
        Look(Right, 1);
        AlineUp();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MoveUp() => Move(Up * MovementSpeed);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MoveDown() => Move(Up * -MovementSpeed);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MoveLeft() => Move(Right * -MovementSpeed);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MoveRight() => Move(Right * MovementSpeed);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SpinClockwise() => Spin(-1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SpinCounterClockwise() => Spin(1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Move(Point3D delta)
    {
        From += delta;
        To += delta;
        // We Don't need to Aline Up b/c we are not changing Direction
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Spin(double degrees) 
        => Up = Up.ApplyAffineTransformation(Utilities.AffineTransformationForRotatingAroundUnit(Direction.Normalized(), Utilities.DegreesToRadians(degrees)));
        // This is changing Up, so it will get "Alined"

    public Point3D Direction => To - From;

    public Point3D Right => Direction.CrossProduct(Up).Normalized();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Look(Point3D around, double degrees) 
        => To = Direction.ApplyAffineTransformation(Utilities.AffineTransformationForRotatingAroundUnit(around, Utilities.DegreesToRadians(degrees), From));

    //adjust it so that it is perpendicular to To-From
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AlineUp(Point3D? direction = null)
    {
        var d = direction ?? Direction;
        m_up = m_up.CrossProduct(d).CrossProduct(d * -1).Normalized();
    }

    private Point3D m_up;
}