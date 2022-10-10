using System.Diagnostics;

class Program
{
    private static readonly Point3D s_from = new (0, TankConstants.EyeHeight, 0);
    private static readonly Point3D s_to = new (0, TankConstants.EyeHeight, 1);
    private static readonly Point3D s_up = Point3D.YUnit;
    private const int c_maxFrameRate = 30;

    static private int Main(string[] args)
    {
        var lastRender = DateTime.UtcNow;
        var runTime = Stopwatch.StartNew();

        Console.CursorVisible = true;

        var map = CharMap.FromString(File.ReadAllText(CharMap.DefaultMapFilePath));

        int windowHorizontal = (Console.WindowWidth - 2) * map.MaxX; // -2 for the border
        int windowVertical = (Console.WindowHeight - 3) * map.MaxY;  // 1 more for the new line at the bottom
        Console.WriteLine($"(({Console.WindowWidth}, {Console.WindowHeight}) - (2, 3)) * ({map.MaxX}, {map.MaxY}) = ({windowHorizontal}, {windowVertical})");

        bool landScapeMode;
        Point2D size;
        if (windowHorizontal > windowVertical)
        {
            size = new(windowVertical, windowVertical);
            landScapeMode = true;
        }
        else
        {
            size = new(windowHorizontal, windowHorizontal);
            landScapeMode = false;
        }
        Console.WriteLine(size);

#if (DEBUG)
        DebugUtilities.Setup(map, size);
#endif

        var scene = new Scene(
            new(s_from, s_to, s_up), 
            size);

        scene.AddActor(new InfinitePlane(ColorProperties.WhitePlastic, y: 0.0));

        scene.AddActor(new Player(Point3D.Zero, ColorUtilities.NamedColor(ConsoleColor.Red)));

        Tank.Create(scene, new(5, 0, 50), ColorUtilities.NamedColor(ConsoleColor.Blue));

        // Just create a light source far off "somewhere"
        scene.AddLightSource(new(
            new(0, Utilities.MaxRange, Utilities.MaxRange / 2),
            ColorUtilities.NamedColor(ConsoleColor.White)
        ));

        s_projectile = Projectile.Create(scene, 
            new(0, TankConstants.EyeHeight, 0),
            ColorUtilities.NamedColor(ConsoleColor.Red),
            new(0, 0, 25));

        Console.Clear();

        Task.Run(AddKeysToQueue);

        int frames = 0;

        var minDelta = TimeSpan.FromSeconds(1.0 / c_maxFrameRate);

        var sleep = new Stopwatch();
        var update = new Stopwatch();

        RenderBase render = new CharRayRender(map, scene, runTime, update, sleep, landScapeMode);

        while (true)
        {
            frames++;
            var now = DateTime.UtcNow;
            var timeDelta = now - lastRender;

            if (timeDelta < minDelta)
            {
                sleep.Start();
                Thread.Sleep(minDelta - timeDelta);
                now = DateTime.UtcNow;
                timeDelta = now - lastRender;
                sleep.Stop();
            }

            lastRender = now;

            update.Start();
            //
            // Adjust Camera based on user input
            if (ConsumeInput(scene))
            {
                break;
            }

            //
            // Adjust our scene based on how much time has passed and user input
            scene.Act(timeDelta, runTime.Elapsed);
            update.Stop();

            render.Render(timeDelta, frames);

#if (DEBUG)
            DebugUtilities.DebugFrame();
#endif
        }

#if (DEBUG)
            Console.WriteLine();
#endif

        return 0;
    }

    static bool ConsumeInput(Scene scene)
    {
        while (s_keys.Count > 0)
        {
            ConsoleKeyInfo info;
            lock (s_keys)
            {
                info = s_keys.Dequeue();
            }
            switch (info.Key)
            {
                case ConsoleKey.Enter:
                    // End program
                    return true;

                case ConsoleKey.W:
                    scene.Camera.MoveForward();
                    break;

                case ConsoleKey.S:
                    scene.Camera.MoveBackward();
                    break;

                case ConsoleKey.D:
                    scene.Camera.TurnRight();
                    break;

                case ConsoleKey.A:
                    scene.Camera.TurnLeft();
                    break;

                case ConsoleKey.V:
                    scene.Camera.AboutFace();
                    break;

                case ConsoleKey.Z:
                    scene.Camera.TurnUp();
                    break;

                case ConsoleKey.Q:
                    scene.Camera.TurnDown();
                    break;

                case ConsoleKey.X:
                    scene.Camera.MoveLeft();
                    break;

                case ConsoleKey.C:
                    scene.Camera.MoveRight();
                    break;

                case ConsoleKey.R:
                    scene.Camera.MoveUp();
                    break; 

                case ConsoleKey.F:
                    scene.Camera.MoveDown();
                    break; 

                case ConsoleKey.T:
                    scene.Camera.SpinClockwise();
                    break;

                case ConsoleKey.G:
                    scene.Camera.SpinCounterClockwise();
                    break;

                case ConsoleKey.E:
                    scene.Camera.ResetPosition(s_from, s_to, s_up);
                    break;

                case ConsoleKey.Spacebar:
                    s_projectile?.Rest();
                    break;

#if (DEBUG)
                case ConsoleKey.D1:
                    DebugUtilities.Enabled = !DebugUtilities.Enabled;
                    break;

                case ConsoleKey.D2:
                    DebugUtilities.MovePointerRight();
                    break;

                case ConsoleKey.D3:
                    DebugUtilities.MovePointerDown();
                    break;

                case ConsoleKey.D4:
                    DebugUtilities.LockInTarget = true;
                    break;

                case ConsoleKey.D5:
                    DebugUtilities.ChangeMarker();
                    break;
#endif
            }
        }
        return false;
    }

    static void AddKeysToQueue()
    {
        if (!Console.IsInputRedirected)
        {
            while (true)
            {
                var i = Console.ReadKey(intercept: true);
                lock (s_keys)
                {
                    s_keys.Enqueue(i);
                }

                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
            }
        }
    }

    private static readonly Queue<ConsoleKeyInfo> s_keys = new();
    private static Projectile? s_projectile;
}
