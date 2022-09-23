#if (DEBUG)

using SixLabors.ImageSharp.PixelFormats;

public static class DebugUtilities
{
    public static void Setup(CharMap map, Point2D size) => s_size = new(
        (int)Math.Ceiling((double)size.H / (double)map.MaxX),
        (int)Math.Ceiling((double)size.V / (double)map.MaxY));

    public static volatile bool Enabled;

    public static volatile bool LockInTarget;

    public static Point2D Pointer {get; private set;} = new(-1, -1);

    public static void MovePointerRight() => Pointer = new(Pointer.H == -1 ? 0 : (Pointer.H + 1) % s_size.H, Pointer.V == -1 ? 0 : Pointer.V);

    public static void MovePointerDown() => Pointer = new(Pointer.H == -1 ? 0 : Pointer.H, Pointer.V == -1 ? 0 : (Pointer.V + 1) % s_size.V);

    public static void ChangeMarker() => s_colorMarker = (s_colorMarker + 1) % ColorUtilities.ConsoleColors.Count();

    public static bool DisplayMark(int x, int y, Actor actor, int id) => MarkPoint(x, y) || (s_objToTrack != null && s_objToTrack.Equals(actor.GetTrackingObjectFromId(id)));

    public static bool MarkPoint(int x, int y) => (Pointer.H == x) && (Pointer.V == y);

    public static bool DebugPoint(int x, int y) => Enabled && MarkPoint(x, y);

    public static void UpdateTrackingTarget(int x, int y, Actor actor, int id)
    {
        if (DebugPoint(x, y) && LockInTarget)
        {
            LockInTarget = false;
            s_objToTrack = actor.GetTrackingObjectFromId(id);
        }
    }

    public static Rgb24 Color => ColorUtilities.NamedColor((ConsoleColor)s_colorMarker); 

    public static bool DebugObject(object obj) => Enabled && s_objToTrack != null && obj.Equals(s_objToTrack);

    public static void DebugFrame()
    {
        Console.ForegroundColor = (ConsoleColor)s_colorMarker;
        Console.SetCursorPosition(0,0);
        Console.Write((Enabled ? 1 : 0) + (LockInTarget ? 2 : 0));

        if (Pointer.H != -1 && Pointer.V != -1)
        {
            Console.SetCursorPosition(Pointer.H + 1, 0);
            Console.Write('┳');
            Console.SetCursorPosition(0, Pointer.V + 1);
            Console.Write('┣');
            Console.SetCursorPosition(s_size.H + 1, Pointer.V + 1);
            Console.Write('┫');
            Console.SetCursorPosition(Pointer.H + 1, s_size.V + 1);
            Console.Write('┻');
        }
        Console.ResetColor();
        Console.SetCursorPosition(s_size.H + 1, s_size.V + 1);
    }

    private static Point2D s_size;

    private static volatile int s_colorMarker = 0;

    private static volatile object? s_objToTrack;
}

#endif