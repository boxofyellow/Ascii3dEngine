using System.Diagnostics;

namespace Ascii3dEngine
{
    public class CharRayRender : RenderBase
    {
        public CharRayRender(CharMap map, Scene scene, Stopwatch runTime, Stopwatch update, Stopwatch sleep, bool landScapeMode) 
            : base(map, scene, runTime, update, sleep, landScapeMode, dataFields: 1)
        {
            m_map = map;
            m_render = new Stopwatch();
        }

        protected override void RenderData()
        {
            m_render.Start();
            (m_lines, _) = Scene.RenderCharRay(Scene.Screen.Size, m_map);
            m_render.Stop();
        }

        protected override void DisplayData()
        {
            for (int i = 0; i < m_lines.Length; i++)
            {
                WriteLine($"│{m_lines[i]}│", includeData: LandscapeMode, i);
            }
        }

        protected override void AddSpecificDiagnosticData()
        {
            AddDataLine($" Render  : {m_render.Elapsed, 25} {(int)(100 * m_render.Elapsed / RunTime.Elapsed), 3}%");
        }

        private CharMap m_map;

        private Stopwatch m_render;

        private string[] m_lines;
    }
}