using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ascii3dEngine.Engine
{
    public abstract class RenderBase
    {
        protected RenderBase(CharMap map, Scene scene, Stopwatch runTime, Stopwatch update, Stopwatch sleep, bool landScapeMode, int dataFields)
        {
            Scene = scene;
            RunTime = runTime;
            m_update = update;
            m_sleep = sleep;
            m_display = new();
            m_diagnosticData = new string[c_numberOfBaseDataItems + dataFields + 1];

            int columns = Utilities.Ratio(scene.Screen.Size.H, map.MaxX);

            m_topBox    = $"┌{new string('─', columns)}┐";
            m_bottomBox = $"└{new string('─', columns)}┘";
            LandscapeMode = landScapeMode;
        }

        public void Render(TimeSpan timeDelta, int frames)
        {
            RenderData();

            UpdateDiagnosticData();

            m_display.Start();

            Console.SetCursorPosition(0,0);
            Console.ResetColor();

            var topRow = $" {timeDelta, 25} {(int)(1.0 /timeDelta.TotalSeconds), 8} fps";
            var bottomRow = $" {RunTime.Elapsed, 25} {frames, 8} fames";

            WriteLine($"{m_topBox}{(LandscapeMode ? topRow : string.Empty)}", includeData: false, row: 0);

            DisplayData();

            WriteLine($"{m_bottomBox}{(LandscapeMode ? bottomRow : string.Empty)}", includeData: false, row: 0);

            if (!LandscapeMode)
            {
                WriteLine(topRow, includeData: false, row: 0);
                for (int i = 0; i < m_diagnosticData.Length; i++)
                {
                    WriteLine(string.Empty, includeData:true, i);
                }
                WriteLine(bottomRow, includeData: false,row: 0);
            }

            m_display.Stop();
        }

        protected abstract void RenderData();

        protected virtual void AddSpecificDiagnosticData() { }

        protected abstract void DisplayData();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void WriteLine(string line, bool includeData, int row)
        {
            var fullLine = $"{line}{(includeData && row < m_diagnosticData.Length ? m_diagnosticData[row] : string.Empty)}";
            Console.WriteLine(fullLine.Length < Console.WindowWidth ? fullLine : fullLine.Substring(0, Console.WindowWidth));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void Write(string line, bool includeData, int row)
        {
            var fullLine = $"{line}{(includeData && row < m_diagnosticData.Length ? m_diagnosticData[row] : string.Empty)}";
            Console.Write(fullLine.Length < Console.WindowWidth ? fullLine : fullLine.Substring(0, Console.WindowWidth));
        }

        protected void AddDataLine(string line)
        {
            if (m_nextDiagnostic == c_invalidDiagnosticIndex || m_nextDiagnostic >= m_diagnosticData.Length)
            {
                throw new InvalidOperationException($"{m_nextDiagnostic} is out of range");
            }
            m_diagnosticData[m_nextDiagnostic++] = line;
        }

        private void UpdateDiagnosticData()
        {
            m_diagnosticData[0] = $" To      : {Scene.Camera.To, 60}";
            m_diagnosticData[1] = $" From    : {Scene.Camera.From, 60}";
            m_diagnosticData[2] = $" Up      : {Scene.Camera.Up, 60}";
            m_diagnosticData[3] = $" Sleep   : {m_sleep.Elapsed, 25} {(int)(100 * m_sleep.Elapsed / RunTime.Elapsed), 3}%";
            m_diagnosticData[4] = $" Update  : {m_update.Elapsed, 25} {(int)(100 * m_update.Elapsed / RunTime.Elapsed), 3}%";

            try
            {
                m_nextDiagnostic = c_numberOfBaseDataItems;
                AddSpecificDiagnosticData();
                AddDataLine($" Display : {m_display.Elapsed, 25} {(int)(100 * m_display.Elapsed / RunTime.Elapsed), 3}%");
            }
            finally
            {
                m_nextDiagnostic = c_invalidDiagnosticIndex;
            }
        }

        protected readonly Scene Scene;
        protected readonly Stopwatch RunTime;
        protected readonly bool LandscapeMode;

        private readonly Stopwatch m_update;
        private readonly Stopwatch m_sleep;
        private readonly Stopwatch m_display;

        private readonly string m_topBox;
        private readonly string m_bottomBox;
        
        private int m_nextDiagnostic = c_invalidDiagnosticIndex;
        private const int c_invalidDiagnosticIndex = -1;
        private const int c_numberOfBaseDataItems = 5;
        private readonly string[] m_diagnosticData;
    }
}