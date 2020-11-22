using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ascii3dEngine
{
    public class LineRender : RenderBase
    {
        public LineRender(Settings settings, CharMap map, Scene scene, Stopwatch runTime, Stopwatch update, Stopwatch sleep, bool landScapeMode) 
            : base(map, scene, runTime, update, sleep, landScapeMode, dataFields: 3)
        {
            m_settings = settings;
            m_map = map;
            m_render = new Stopwatch();
            m_fit = new Stopwatch();
            m_label = new Stopwatch();

            m_labelRows = new List<Label>[scene.Screen.Size.V / map.MaxY + 1];
        }

        protected override void RenderData()
        {
            m_render.Start();
            //
            // Render our scene in into a 2D image, creating a 2D boolean array for which places have a line
            (bool[,] imageData, List<Label> labels) = Scene.Render();
            m_render.Stop();

            m_fit.Start();
            //
            // Change 2D boolean array into an array of character
            CharacterFitter fitter = CharacterFitter.Create(m_settings, imageData, m_map);
            m_lines = fitter.ComputeChars(m_settings);
            m_fit.Stop();

            m_label.Start();
            //
            // Organize Lables
            foreach (List<Label> labelsForRow in m_labelRows)
            {
                labelsForRow?.Clear();
            }
            foreach (Label label in labels)
            {
                if (label.Row < m_labelRows.Length)
                {
                    (m_labelRows[label.Row] ??= new List<Label>()).Add(label);
                }
            } 
            m_label.Stop();
        }

        protected override void DisplayData()
        {
            for (int i = 0; i < m_lines.Length; i++)
            {
                Write($"│{m_lines[i]}│", includeData: LandscapeMode, i);
                if (m_labelRows[i]?.Any() ?? false)
                {
                    foreach(Label label in m_labelRows[i])
                    {
                        Console.ForegroundColor = label.Foreground;
                        Console.BackgroundColor = label.Background;
                        Console.SetCursorPosition(Math.Min(label.Column + 1, m_lines[0].Length), Math.Min(label.Row + 1, m_lines.Length));
                        Console.Write(label.Character);
                    }
                    Console.ResetColor();
                }
                Console.WriteLine();
            }
        }

        protected override void AddSpecificDiagnosticData()
        {
            AddDataLine($" Render  : {m_render.Elapsed, 25} {(int)(100 * m_render.Elapsed / RunTime.Elapsed), 3}%");
            AddDataLine($" Fit     : {m_fit.Elapsed, 25} {(int)(100 * m_fit.Elapsed / RunTime.Elapsed), 3}%");
            AddDataLine($" Label   : {m_label.Elapsed, 25} {(int)(100 * m_label.Elapsed / RunTime.Elapsed), 3}%");
        }

        private Settings m_settings;
        private CharMap m_map;

        private Stopwatch m_render;
        private Stopwatch m_fit;
        private Stopwatch m_label;

        private List<Label>[] m_labelRows;
        private string[] m_lines;
    }
}