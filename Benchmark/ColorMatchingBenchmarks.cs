using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Ascii3dEngine.Benchmark
{
    /*
    From the last run
|        Method |     N |        Mean |     Error |    StdDev |
|-------------- |------ |------------:|----------:|----------:|
| FindAllColors |     0 |   300.08 ms |  4.968 ms |  4.647 ms |
| FindAllColors |     1 |   570.48 ms | 11.226 ms | 17.806 ms |
| FindAllColors |     2 |   327.64 ms |  6.466 ms |  9.274 ms |
| FindAllColors |     4 |   178.71 ms |  3.556 ms |  4.747 ms |
| FindAllColors |     8 |   101.18 ms |  1.915 ms |  2.205 ms |
| FindAllColors |    16 |    65.53 ms |  1.266 ms |  1.971 ms |
| FindAllColors |    32 |    46.91 ms |  0.933 ms |  1.111 ms |
| FindAllColors |    64 |    40.81 ms |  0.790 ms |  0.879 ms |
| FindAllColors |   128 |    39.01 ms |  0.778 ms |  1.012 ms |
| FindAllColors |   256 |    44.01 ms |  0.825 ms |  0.847 ms |
| FindAllColors |   512 |    58.06 ms |  1.065 ms |  0.996 ms |
| FindAllColors |  1024 |    71.45 ms |  1.373 ms |  1.686 ms |
| FindAllColors |  2048 |   266.75 ms |  5.126 ms |  6.483 ms |
| FindAllColors |  4096 |   329.04 ms |  6.416 ms |  7.637 ms |
| FindAllColors |  8192 |   326.80 ms |  6.266 ms |  5.861 ms |
| FindAllColors | 16384 | 2,424.07 ms | 25.714 ms | 24.053 ms |
    */

    //[Config(typeof(TestFlagConfig))]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [MarkdownExporter, AsciiDocExporter, HtmlExporter, CsvExporter, RPlotExporter]
    public class ColorMatchingBenchmarks
    {
        //[Params(0)]
        [Params(0, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            // This is really here to make sure we get this static info loaded before running out test
            if (StaticColorValidationData.TestColors.Length == 0)
            {
                Console.WriteLine("This should never show up");
            }

            // N == 0 will indicate that we should used the Crazy Match
            if (N > 0)
            {
                m_octree = StaticColorValidationData.CreateOctree(N);
            }
        }

        [Benchmark]
        public void FindAllColors()
        {
            for (int i = 0; i < StaticColorValidationData.TestColors.Length; i++)
            {
                if (N == 0)
                {
                    ColorUtilities.BestMatch(StaticColorValidationData.Map, StaticColorValidationData.TestColors[i]);
                }
                else
                {
                    m_octree.BestMatch(StaticColorValidationData.TestColors[i]);
                }
            }
        }

        private ColorOctree m_octree;
    }
}