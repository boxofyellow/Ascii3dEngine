using System.Runtime.CompilerServices;

public readonly struct Point2D
{
    public readonly int H, V;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point2D(int h, int v)
    {
        H = h; V = v;
    }

    public override string ToString() => $"{{{H}, {V}}}";
}