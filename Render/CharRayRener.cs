using System;
using System.Collections.Generic;
using System.Diagnostics;
using SixLabors.ImageSharp.PixelFormats;

namespace Ascii3dEngine
{
    public class CharRayRender : RenderBase
    {
        public CharRayRender(CharMap map, Scene scene, Stopwatch runTime, Stopwatch update, Stopwatch sleep, bool landScapeMode) 
            : base(map, scene, runTime, update, sleep, landScapeMode, dataFields: 2)
        {
            m_map = map;
            m_render = new Stopwatch();
            m_match = new Stopwatch();

            m_hight = Utilities.Ratio(scene.Screen.Size.V, map.MaxY);
            m_width = Utilities.Ratio(scene.Screen.Size.H, map.MaxX);
            m_buffer = new char[m_hight * m_width];
            m_colors = new ColorInfo[m_hight * m_width];
        }

        protected override void RenderData()
        {
            m_render.Start();
            Rgb24[,] colorData = Scene.RenderCharRayColor(Scene.Screen.Size, m_map); 
            m_render.Stop();

            m_match.Start();

            int bufferIndex = default;
            int colorIndex = -1;  // makes it easy to handle the first one

            for (int y = default; y < m_hight; y++)
            for (int x = default; x < m_width; x++)
            {
                if (!m_cache.TryGetValue(colorData[x, y], out var match))
                {
                    match = ColorUtilities.BestMatch(m_map, colorData[x, y]);
                    m_cache.Add(colorData[x, y], match);
                }

                m_buffer[bufferIndex++] = match.Character;

                if (x != default && m_colors[colorIndex].Match(match.Background, match.Foreground, match.Character))
                {
                    m_colors[colorIndex].Length++;
                }
                else
                {
                    colorIndex++;
                    m_colors[colorIndex] = new ColorInfo(match.Background, match.Foreground);
                }
            }
            m_match.Stop();
        }

        protected override void DisplayData()
        {
            int row = default;
            int column = default;

            int bufferIndex = default;
            int colorIndex = default;

            while(true)
            {
                if (column == default)
                {
                    Console.Write('│');
                }

                int length = m_colors[colorIndex++].Apply(bufferIndex, m_buffer);
                bufferIndex += length;
                column += length;

                if (column == m_width)
                {
                    Console.ResetColor();
                    WriteLine("│", includeData: LandscapeMode, row);
                    column = default;
                    row++;
                    if (row == m_hight)
                    {
                        break;
                    }
                }
                else if (column > m_width)
                {
                    throw new ApplicationException("How did this happen");
                }
            }
        }

        protected override void AddSpecificDiagnosticData()
        {
            AddDataLine($" Render  : {m_render.Elapsed, 25} {(int)(100 * m_render.Elapsed / RunTime.Elapsed), 3}%");
            AddDataLine($" Match   : {m_match.Elapsed, 25} {(int)(100 * m_match.Elapsed / RunTime.Elapsed), 3}%");
        }

        private CharMap m_map;
        private Stopwatch m_render;
        private Stopwatch m_match;
        private readonly int m_hight;
        private readonly int m_width;

        // We could use these to spreed the display by skiping things stuff that
        // is not changing, but don't appear to be spending much time there.
        private readonly char[] m_buffer;

        private struct ColorInfo
        {
            public ColorInfo(ConsoleColor background, ConsoleColor foreground)
            {
                Background = background;
                Foreground = foreground;
                Length = 1;
            }

            public readonly ConsoleColor Background;
            public readonly ConsoleColor Foreground;
            public int Length;

            public bool Match(ConsoleColor otherBack, ConsoleColor otherFore, char nextChar) 
                => (otherBack == Background) && ((otherFore == Foreground) || (nextChar == ' '));

            public int Apply(int start, char[] buffer)
            {
                Console.BackgroundColor = Background;
                Console.ForegroundColor = Foreground;
                Console.Write(buffer, start, Length);
                return Length;
            }
        }
        private readonly ColorInfo[] m_colors;

        private Dictionary<Rgb24, (Char Character, ConsoleColor Foreground, ConsoleColor Background, Rgb24 Result)> m_cache 
            = new Dictionary<Rgb24, (char Character, ConsoleColor Foreground, ConsoleColor Background, Rgb24 Result)>();
    }
}