using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace Ascii3dEngine
{
    class Program
    {

        static private int Main(string[] args) => Parser.Default.ParseArguments<Settings>(args).MapResult(Run, HandleParseError);

        static private int HandleParseError(IEnumerable<Error> errs) => 100;
        
        public static int Run(Settings settings)
        {
            DateTime lastRender = DateTime.UtcNow;
            Stopwatch runTime = Stopwatch.StartNew();

            Console.CursorVisible = true;

            m_map = new CharMap();
            int windowHorizontal = (Console.WindowWidth - 2) * m_map.MaxX; // -2 for the border
            int windowVertical = (Console.WindowHeight - 3) * m_map.MaxY;  // 1 more for the new line at the bottom
            Console.WriteLine($"(({Console.WindowHeight}, {Console.LargestWindowWidth}) - (2, 3)) * ({m_map.MaxX}, {m_map.MaxY}) = ({windowHorizontal}, {windowVertical})");

            Point2D size;
            if (windowHorizontal > windowVertical)
            {
                size = new Point2D(
                    windowVertical * 1.75, // I'm not sure where this factor is coming from 
                                           // maybe we are correctly account for the space overed by a character
                    windowVertical);
            }
            else
            {
                size = new Point2D(
                    windowHorizontal,
                    windowHorizontal);
            }
            Console.WriteLine(size);

            m_scene = new Scene(settings, size);
            if (settings.Spin && string.IsNullOrEmpty(settings.ModelFile))
            {
                settings.Cube = true;
            }

            if (!settings.Axes && !settings.Cube && string.IsNullOrEmpty(settings.ModelFile))
            {
                settings.Axes = true;
            }

            if (settings.Axes)
            {
                m_scene.AddActor(new Axes(m_map));
            }

            if (settings.Cube)
            {
                m_scene.AddActor(new Cube(settings, m_map));
            }

            if (!string.IsNullOrEmpty(settings.ModelFile))
            {
                m_scene.AddActor(new Model(settings));
            }

            Console.Clear();

            Task.Run(AddKeysToQueue);

            int frames = 0;

            TimeSpan minDelta = settings.MaxFrameRate > 0
                ? TimeSpan.FromSeconds(1.0 / settings.MaxFrameRate)
                : TimeSpan.Zero;

            Stopwatch sleep = new Stopwatch();
            Stopwatch update = new Stopwatch();
            Stopwatch render = new Stopwatch();
            Stopwatch fit = new Stopwatch();
            Stopwatch display = new Stopwatch();

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
                if (ConsumeInput())
                {
                    break;
                }

                //
                // Adjust our scene based on how much time has passed and user input
                m_scene.Act(timeDelta, runTime.Elapsed);
                update.Stop();

                render.Start();
                //
                // Render our scene in into a 2D image, creating a 2D boolean array for which places have a line
                (bool[,] imageData, List<Label> labels) = m_scene.Render();
                render.Stop();

                fit.Start();
                //
                // Change 2D boolean array into an array of character
                CharacterFitter fitter = CharacterFitter.Create(settings, imageData, m_map);
                string[] lines = fitter.ComputeChars(settings);
                fit.Stop();


                string[] data = new[]
                {
                    $" To      : {m_scene.Camera.To, 75}",
                    $" From    : {m_scene.Camera.From, 75}",
                    $" Up      : {m_scene.Camera.Up, 75}",
                    $" Sleep   : {sleep.Elapsed, 25} {(int)(100 * sleep.Elapsed / runTime.Elapsed), 3}%",
                    $" Update  : {update.Elapsed, 25} {(int)(100 * update.Elapsed / runTime.Elapsed), 3}%",
                    $" Render  : {render.Elapsed, 25} {(int)(100 * render.Elapsed / runTime.Elapsed), 3}%",
                    $" Fit     : {fit.Elapsed, 25} {(int)(100 * fit.Elapsed / runTime.Elapsed), 3}%",
                    $" Display : {display.Elapsed, 25} {(int)(100 * display.Elapsed / runTime.Elapsed), 3}%",
                };

                display.Start();
                //
                // Draw our lines to the screen
                Console.SetCursorPosition(0,0);
                Console.WriteLine($"┌{new string('─', lines[0].Length)}┐ {timeDelta, 25} {(int)(1.0 /timeDelta.TotalSeconds), 4} fps");
                for (int i = 0; i < lines.Length; i++)
                {
                    Console.WriteLine($"│{lines[i]}│{(i < data.Length ? data[i] : "")}");
                }
                Console.WriteLine($"└{new string('─', lines[0].Length)}┘ {runTime.Elapsed, 25} {frames, 8} fames");

                if (labels.Any())
                {
                    ConsoleColor foreground = Console.ForegroundColor;
                    try
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        foreach(Label label in labels)
                        {
                            Console.SetCursorPosition(Math.Min(label.Column + 1, lines[0].Length), Math.Min(label.Row + 1, lines.Length));
                            Console.Write(label.Character);
                        }
                    }
                    finally
                    {
                        Console.ForegroundColor = foreground;
                        Console.SetCursorPosition(0, lines.Length + 2);
                    }
                }
                display.Stop();
            }

            return 0;
        }

        static bool ConsumeInput()
        {
            while (m_keys.Count > 0)
            {
                ConsoleKeyInfo info;
                lock (m_keys)
                {
                    info = m_keys.Dequeue();
                }
                switch (info.Key)
                {
                    case ConsoleKey.Enter:
                        // End program
                        return true;

                    case ConsoleKey.W:
                        m_scene.Camera.MoveForward();
                        break;

                    case ConsoleKey.S:
                        m_scene.Camera.MoveBackward();
                        break;

                    case ConsoleKey.D:
                        m_scene.Camera.TurnRight();
                        break;

                    case ConsoleKey.A:
                        m_scene.Camera.TurnLeft();
                        break;

                    case ConsoleKey.V:
                        m_scene.Camera.AboutFace();
                        break;

                    case ConsoleKey.Z:
                        m_scene.Camera.TurnUp();
                        break;

                    case ConsoleKey.Q:
                        m_scene.Camera.TurnDown();
                        break;

                    case ConsoleKey.X:
                        m_scene.Camera.MoveLeft();
                        break;

                    case ConsoleKey.C:
                        m_scene.Camera.MoveRight();
                        break;

                    case ConsoleKey.R:
                        m_scene.Camera.MoveUp();
                        break; 

                    case ConsoleKey.F:
                        m_scene.Camera.MoveDown();
                        break; 

                    case ConsoleKey.T:
                        m_scene.Camera.SpinClockwise();
                        break;

                    case ConsoleKey.G:
                        m_scene.Camera.SpinCounterClockwise();
                        break;

                    case ConsoleKey.E:
                        m_scene.Camera.ResetPosition();
                        break;
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
                    lock (m_keys)
                    {
                        m_keys.Enqueue(i);
                    }

                    if (i.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                }
            }
        }

        private static CharMap m_map;
        private static Scene m_scene;
        static Queue<ConsoleKeyInfo> m_keys = new Queue<ConsoleKeyInfo>();
    }
}