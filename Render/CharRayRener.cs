using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using SixLabors.ImageSharp.PixelFormats;

namespace Ascii3dEngine
{
    public class CharRayRender : RenderBase
    {
        public CharRayRender(CharMap map, Scene scene, Stopwatch runTime, Stopwatch update, Stopwatch sleep, bool landScapeMode) 
            : base(map, scene, runTime, update, sleep, landScapeMode, dataFields: 4)
        {
            m_map = map;
            m_render = new Stopwatch();
            m_match = new Stopwatch();

            m_hight = Utilities.Ratio(scene.Screen.Size.V, map.MaxY);
            m_width = Utilities.Ratio(scene.Screen.Size.H, map.MaxX);

            m_total = m_hight * m_width;

            m_buffer = new char[m_total];
            m_colors = new ColorInfo[m_total];
            m_lastBuffer = new char[m_total];
            m_lastColors = new ColorInfo[m_total];
        }

        protected override void RenderData()
        {
            m_render.Start();
            var colorData = Scene.RenderCharRayColor(Scene.Screen.Size, m_map); 
            m_render.Stop();

            var tempBuffer = m_lastBuffer;
            var tempColors = m_lastColors;
            m_lastBuffer = m_buffer;
            m_lastColors = m_colors;
            m_buffer = tempBuffer;
            m_colors = tempColors;

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
                    m_colors[colorIndex] = new(match.Background, match.Foreground);
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
            int lastColorIndex = default;
            var lastColor = m_lastColors[lastColorIndex++];
            int left = lastColor.Length;

            m_static = default;

            while(true)
            {
                if (column == default)
                {
                    Console.Write('│');
                }

                (int length, bool changed) = m_colors[colorIndex++].Apply(bufferIndex, column, row, m_buffer, m_lastBuffer, lastColor.Background, lastColor.Foreground, left);
                bufferIndex += length;
                column += length; 

                if (!changed)
                {
                    m_static += length;
                }

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

                if (lastColor.Length != default)
                {
                    // We need to check to make sure we are not on the "first" pass since last is not filled in
                    // in which case Length on last won't be set either
                    left -= length;
                    while (left <= 0)
                    {
                        lastColor = m_lastColors[lastColorIndex++];
                        left += lastColor.Length;
                    }
                }
            }
        }

        protected override void AddSpecificDiagnosticData()
        {
            AddDataLine($" Render  : {m_render.Elapsed, 25} {(int)(100 * m_render.Elapsed / RunTime.Elapsed), 3}%");
            AddDataLine($" Colors  : {m_cache.Count}");
            AddDataLine($" Match   : {m_match.Elapsed, 25} {(int)(100 * m_match.Elapsed / RunTime.Elapsed), 3}%");
            AddDataLine($" Static  : {(int)(100 * m_static / m_total), 3}%");
        }

        private readonly CharMap m_map;
        private readonly Stopwatch m_render;
        private readonly Stopwatch m_match;
        private readonly int m_hight;
        private readonly int m_width;

        private char[] m_buffer;

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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public (int, bool) Apply(int start, int column, int row, char[] buffer, char[] lastBuffer, ConsoleColor background, ConsoleColor foreground, int length)
            {
                bool changed = background != Background
                            || foreground != Foreground
                            || Length > length;

                if (!changed)
                {
                    for (int i = default; i < Length; i++)
                    {
                        int index = i + start;
                        if (buffer[index] != lastBuffer[index])
                        {
                            changed = true;
                            break;
                        }
                    }
                }

                if (changed)
                {
                    Console.BackgroundColor = Background;
                    Console.ForegroundColor = Foreground;
                    Console.Write(buffer, start, Length);
                }
                else
                {
                    // I think we might be able to eak-out a little more saving if we did this before we write, and only when needed
                    // But it looks like this does not cost that much, just commenting it out the UI looks crazy, but it really does
                    // not impact the run time
                    Console.SetCursorPosition(column + 1 + Length, row + 1);
                }

                return (Length, changed);
            }
        }

        private ColorInfo[] m_colors;

        // We used there cut down time spent doing display, if the colors is not chaning then we don't have to change it
        // For images that are not chaning we cut display down to less then 50%, when things change we still get savings, just not that much
        private char[] m_lastBuffer;
        private ColorInfo[] m_lastColors;

        private int m_static;
        private readonly int m_total;

        private readonly Dictionary<Rgb24, (char Character, ConsoleColor Foreground, ConsoleColor Background, Rgb24 Result)> m_cache = new();
    }
}