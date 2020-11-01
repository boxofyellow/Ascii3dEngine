using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Ascii3dEngine.Benchmark
{
    /*
    From the last run
|        Method |     N |        Mean |     Error |    StdDev |
|-------------- |------ |------------:|----------:|----------:|
| FindAllColors |     0 |   212.91 ms |  4.011 ms |  4.619 ms |
| FindAllColors |     1 |   573.04 ms | 11.423 ms | 21.174 ms |
| FindAllColors |     2 |   329.64 ms |  6.374 ms |  9.541 ms |
| FindAllColors |     4 |   178.93 ms |  3.503 ms |  3.748 ms |
| FindAllColors |     8 |   100.53 ms |  2.000 ms |  2.804 ms |
| FindAllColors |    16 |    65.10 ms |  1.299 ms |  1.643 ms |
| FindAllColors |    32 |    47.12 ms |  0.919 ms |  0.944 ms |
| FindAllColors |    64 |    41.27 ms |  0.816 ms |  1.032 ms |
| FindAllColors |   128 |    38.60 ms |  0.618 ms |  0.662 ms |
| FindAllColors |   256 |    43.84 ms |  0.832 ms |  0.959 ms |
| FindAllColors |   512 |    58.48 ms |  1.147 ms |  1.275 ms |
| FindAllColors |  1024 |    71.40 ms |  1.372 ms |  1.284 ms |
| FindAllColors |  2048 |   266.06 ms |  5.303 ms |  5.895 ms |
| FindAllColors |  4096 |   327.55 ms |  5.368 ms |  5.021 ms |
| FindAllColors |  8192 |   326.84 ms |  6.189 ms |  6.078 ms |
| FindAllColors | 16384 | 2,422.45 ms | 15.269 ms | 14.283 ms |
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