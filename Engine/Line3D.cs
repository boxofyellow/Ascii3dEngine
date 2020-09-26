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
}