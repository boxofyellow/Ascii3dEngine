using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Ascii3dEngine.Tanks
{
    class Program
    {
        static private int Main(string[] args)
        {
            DateTime lastRender = DateTime.UtcNow;
            Stopwatch runTime = Stopwatch.StartNew();

            Console.CursorVisible = true;

            Settings settings = new Settings
            {
                UseCharRay = true,
                From = new Point3D(0, TankConstancts.EyeHeight, 0).ToString(),
                To = new Point3D(10, TankConstancts.EyeHeight, 0).ToString(),
                MaxFrameRate = 30,
            };

            CharMap map = new CharMap(settings);

            int windowHorizontal = (Console.WindowWidth - 2) * map.MaxX; // -2 for the border
            int windowVertical = (Console.WindowHeight - 3) * map.MaxY;  // 1 more for the new line at the bottom
            Console.WriteLine($"(({Console.WindowWidth}, {Console.WindowHeight}) - (2, 3)) * ({map.MaxX}, {map.MaxY}) = ({windowHorizontal}, {windowVertical})");

            bool landScapeMode;
            Point2D size;
            if (windowHorizontal > windowVertical)
            {
                size = new Point2D(
                    (int)(windowVertical * Utilities.FudgeFactor),
                    windowVertical);
                landScapeMode = true;
            }
            else
            {
                size = new Point2D(
                    windowHorizontal,
                    (int)(windowHorizontal / Utilities.FudgeFactor));
                landScapeMode = false;
            }
            Console.WriteLine(size);

#if (DEBUG)
            DebugUtilities.Setup(map, size);
#endif

            Scene scene = new Scene(settings, size);

            scene.AddActor(new InfinitePlane(settings, ColorProperties.WhitePlastic, y: 0.0));

            // Just create a light source far off "somewhere"
            scene.AddLightSource(new LightSource(
                new Point3D(0, Utilities.MaxRange, Utilities.MaxRange / 2),
                ColorUtilities.NamedColor(ConsoleColor.White)
            ));

            Projectile.Create(scene, 
                new Point3D(0, TankConstancts.EyeHeight, 0),
                ColorUtilities.NamedColor(ConsoleColor.Red),
                ColorProperties.RedPlastic,
                new Point3D(2, 0, 0));

            Console.Clear();

            Task.Run(AddKeysToQueue);

            int frames = 0;

            TimeSpan minDelta = settings.MaxFrameRate > 0
                ? TimeSpan.FromSeconds(1.0 / settings.MaxFrameRate)
                : TimeSpan.Zero;

            Stopwatch sleep = new Stopwatch();
            Stopwatch update = new Stopwatch();

            RenderBase render = settings.UseCharRay
                ? (RenderBase)new CharRayRender(map, scene, runTime, update, sleep, landScapeMode)
                : (RenderBase)new LineRender(settings, map, scene, runTime, update, sleep, landScapeMode);

            while (true)
            {
                frames++;
                DateTime now = DateTime.UtcNow;
                TimeSpan timeDelta = now - lastRender;

                if (settings.MaxFrameRate > 0 && timeDelta < minDelta)
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
                        scene.Camera.ResetPosition();
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
                    ConsoleKeyInfo i = Console.ReadKey(intercept: true);
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

        static Queue<ConsoleKeyInfo> s_keys = new Queue<ConsoleKeyInfo>();
    }
}
