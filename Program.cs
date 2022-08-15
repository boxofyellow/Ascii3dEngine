using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace Ascii3dEngine
{
    class Program
    {

        static private int Main(string[] args) => Parser.Default.ParseArguments<Settings>(args).MapResult(Run, HandleParseError);

        private static int GenerateCounts()
        {
            Console.WriteLine("Generating Counts");
            ColorUtilities.BruteForce.Counting();
            return 0;
        }

        static private int HandleParseError(IEnumerable<Error> errs) => 100;

        private static int Run(Settings settings)
        {

#if (GENERATECOUNTS)
            if (GenerateCounts() == 0)
            {
                return 0;
            }
#endif

            var lastRender = DateTime.UtcNow;
            var runTime = Stopwatch.StartNew();

            Console.CursorVisible = true;

            var map = new CharMap(settings);

            int windowHorizontal = (Console.WindowWidth - 2) * map.MaxX; // -2 for the border
            int windowVertical = (Console.WindowHeight - 3) * map.MaxY;  // 1 more for the new line at the bottom
            Console.WriteLine($"(({Console.WindowWidth}, {Console.WindowHeight}) - (2, 3)) * ({map.MaxX}, {map.MaxY}) = ({windowHorizontal}, {windowVertical})");

            bool landScapeMode;
            Point2D size;
            if (windowHorizontal > windowVertical)
            {
                size = new(
                    (int)(windowVertical * Utilities.FudgeFactor),
                    windowVertical);
                landScapeMode = true;
            }
            else
            {
                size = new(
                    windowHorizontal,
                    (int)(windowHorizontal / Utilities.FudgeFactor));
                landScapeMode = false;
            }
            Console.WriteLine(size);

#if (DEBUG)
            DebugUtilities.Setup(map, size);
#endif

            var scene = new Scene(settings, size);
            if (!settings.Axes && !settings.Cube && !settings.ColorChart && string.IsNullOrEmpty(settings.ModelFile))
            {
                settings.Axes = true;
                settings.Cube = true;
            }

            if (settings.Axes)
            {
                scene.AddActor(new Axes(settings, map));
            }

            if (settings.Cube)
            {
                scene.AddActor(new Cube(settings, map));
            }

            if (settings.ColorChart)
            {
                scene.AddActor(new ColorChart(settings, map));
            }

            if (!string.IsNullOrEmpty(settings.ModelFile))
            {
                scene.AddActor(new Model(settings));
            }

            scene.AddActor(new CheckeredInfinitePlane(settings, ColorProperties.WhitePlastic, ColorProperties.BluePlastic, y: -30.0));

            scene.AddLightSource(new(
                new(0, 200, 0),
                ColorUtilities.NamedColor(ConsoleColor.White)
            ));

            Console.Clear();

            Task.Run(AddKeysToQueue);

            int frames = 0;

            TimeSpan minDelta = settings.MaxFrameRate > 0
                ? TimeSpan.FromSeconds(1.0 / settings.MaxFrameRate)
                : TimeSpan.Zero;

            var sleep = new Stopwatch();
            var update = new Stopwatch();

            RenderBase render = settings.UseCharRay
                ? new CharRayRender(map, scene, runTime, update, sleep, landScapeMode)
                : new LineRender(settings, map, scene, runTime, update, sleep, landScapeMode);

            while (true)
            {
                frames++;
                var now = DateTime.UtcNow;
                var timeDelta = now - lastRender;

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

        static readonly Queue<ConsoleKeyInfo> s_keys = new();
    }
}