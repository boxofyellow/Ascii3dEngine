using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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

            Scene scene = new Scene(settings, size);
            if (!settings.Axes && !settings.Cube && !settings.ColorChart && string.IsNullOrEmpty(settings.ModelFile))
            {
                settings.Axes = true;
                settings.Cube = true;
            }

            if (settings.Axes)
            {
                scene.AddActor(new Axes(map));
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

            List<Label>[] labelRows = new List<Label>[size.V / map.MaxY + 1];

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

                render.Start();
                string[] lines;
                List<Label> labels;
                if (settings.UseCharRay)
                {
                    (lines, labels) = scene.RenderCharRay(size, map);
                    render.Stop();
                }
                else
                {
                    //
                    // Render our scene in into a 2D image, creating a 2D boolean array for which places have a line
                    bool[,] imageData;
                    (imageData, labels) = scene.Render();
                    render.Stop();

                    fit.Start();
                    //
                    // Change 2D boolean array into an array of character
                    CharacterFitter fitter = CharacterFitter.Create(settings, imageData, map);
                    lines = fitter.ComputeChars(settings);
                    fit.Stop();
                }

                foreach (List<Label> labelsForRow in labelRows)
                {
                    if (labelsForRow != null)
                    {
                        labelsForRow.Clear();
                    }
                }
                foreach (Label label in labels)
                {
                    if (label.Row < labelRows.Length)
                    {
                        labelRows[label.Row] ??= new List<Label>();
                        labelRows[label.Row].Add(label);
                    }
                }

                string[] data = new[]
                {
                    $" To      : {scene.Camera.To, 60}",
                    $" From    : {scene.Camera.From, 60}",
                    $" Up      : {scene.Camera.Up, 60}",
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
                string topRow = $" {timeDelta, 25} {(int)(1.0 /timeDelta.TotalSeconds), 8} fps";
                string bottomRow = $" {runTime.Elapsed, 25} {frames, 8} fames";

                WriteLine($"┌{new string('─', lines[0].Length)}┐{(landScapeMode ? topRow : string.Empty)}", includeData: false, data, row: 0);
                for (int i = 0; i < lines.Length; i++)
                {
                    Write($"│{lines[i]}│", includeData: landScapeMode, data, i);
                    if (labelRows[i] != null && labelRows[i].Any())
                    {
                        foreach(Label label in labelRows[i])
                        {
                            Console.ForegroundColor = label.Foreground;
                            Console.BackgroundColor = label.Background;
                            Console.SetCursorPosition(Math.Min(label.Column + 1, lines[0].Length), Math.Min(label.Row + 1, lines.Length));
                            Console.Write(label.Character);
                        }
                        Console.ResetColor();
                    }
                    Console.WriteLine();
                }
                WriteLine($"└{new string('─', lines[0].Length)}┘{(landScapeMode ? bottomRow : string.Empty)}", includeData: false, data, row: 0);
                if (!landScapeMode)
                {
                    WriteLine(topRow, includeData: false, data, row: 0);
                    for (int i = 0; i < data.Length; i++)
                    {
                        WriteLine(string.Empty, includeData:true, data, i);
                    }
                    WriteLine(bottomRow, includeData: false, data, row: 0);
                }
                display.Stop();
            }

            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void WriteLine(string line, bool includeData, string[] data, int row)
        {
            string fullLine = $"{line}{(includeData && row < data.Length ? data[row] : string.Empty)}";
            Console.WriteLine(fullLine.Length < Console.WindowWidth ? fullLine : fullLine.Substring(0, Console.WindowWidth));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Write(string line, bool includeData, string[] data, int row)
        {
            string fullLine = $"{line}{(includeData && row < data.Length ? data[row] : string.Empty)}";
            Console.Write(fullLine.Length < Console.WindowWidth ? fullLine : fullLine.Substring(0, Console.WindowWidth));
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