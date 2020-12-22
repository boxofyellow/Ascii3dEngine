#if (DEBUG)

using System;
using System.Linq;
using SixLabors.ImageSharp.PixelFormats;

namespace Ascii3dEngine
{
    public static class DebugUtilities
    {
        public static void Setup(CharMap map, Point2D size) => s_size = new Point2D(
            (int)Math.Ceiling((double)size.H / (double)map.MaxX),
            (int)Math.Ceiling((double)size.V / (double)map.MaxY));

        public static bool Enabled {get; set;}

        public static bool LockInTarget {get; set;}

        public static Point2D Pointer {get; private set;} = new Point2D(-1, -1);

        public static object ObjToTrack {get; private set; }

        public static void MovePointerRight() => Pointer = new Point2D(Pointer.H == -1 ? 0 : (Pointer.H + 1) % s_size.H, Pointer.V == -1 ? 0 : Pointer.V);

        public static void MovePointerDown() => Pointer = new Point2D(Pointer.H == -1 ? 0 : Pointer.H, Pointer.V == -1 ? 0 : (Pointer.V + 1) % s_size.V);

        public static void ChangeMarker() => s_colorMarker = (s_colorMarker + 1) % ColorUtilities.ConsoleColors.Count();

        public static bool MarkPoint(int x, int y) => (Pointer.H == x) && (Pointer.V == y);

        public static bool DebugPoint(int x, int y) => Enabled && MarkPoint(x, y);

        public static void UpdateTrackingTarget(int x, int y, Actor actor, int id)
        {
            if (DebugPoint(x, y) && LockInTarget)
            {
                LockInTarget = false;
                ObjToTrack = actor.GetTrackingObjectFromId(id);
            }
        }

        public static Rgb24 Color => ColorUtilities.NamedColor((ConsoleColor)s_colorMarker); 

        public static bool DebugObject(object obj) => Enabled && ObjToTrack != null && obj == ObjToTrack;

        public static void DebugFrame()
        {
            Console.ForegroundColor = (ConsoleColor)s_colorMarker;
            Console.SetCursorPosition(0,0);
            Console.Write(Enabled ? 1 : 0);

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

        private static int s_colorMarker = 0;
    }
}

#endif