using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Ascii3dEngine.Benchmark
{
    /*
    From the last run
|        Method |     N |        Mean |     Error |    StdDev |
|-------------- |------ |------------:|----------:|----------:|
| FindAllColors |     0 |   338.16 ms |  4.008 ms |  3.347 ms |
| FindAllColors |     1 |   490.04 ms |  9.647 ms |  9.023 ms |
| FindAllColors |     2 |   307.62 ms |  5.811 ms |  6.218 ms |
| FindAllColors |     4 |   176.56 ms |  3.526 ms |  4.943 ms |
| FindAllColors |     8 |   102.43 ms |  2.029 ms |  2.492 ms |
| FindAllColors |    16 |    64.96 ms |  1.286 ms |  1.579 ms |
| FindAllColors |    32 |    47.92 ms |  0.949 ms |  1.559 ms |
| FindAllColors |    64 |    42.32 ms |  0.838 ms |  1.615 ms |
| FindAllColors |   128 |    41.59 ms |  0.817 ms |  1.296 ms |
| FindAllColors |   256 |    46.14 ms |  0.919 ms |  1.770 ms |
| FindAllColors |   512 |    59.79 ms |  1.189 ms |  1.987 ms |
| FindAllColors |  1024 |    75.47 ms |  1.496 ms |  1.945 ms |
| FindAllColors |  2048 |   264.33 ms |  5.209 ms |  6.773 ms |
| FindAllColors |  4096 |   322.71 ms |  5.436 ms |  5.816 ms |
| FindAllColors |  8192 |   339.63 ms |  5.862 ms |  4.895 ms |
| FindAllColors | 16384 | 2,423.93 ms | 19.472 ms | 18.214 ms |
    */
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [MarkdownExporter, AsciiDocExporter, HtmlExporter, CsvExporter, RPlotExporter]
    public class Benchmarks
    {
        [Params(0, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            // This is really here to make sure we get this static info loaded before running out test
            if (StaticColorValidationData.TestColors.Length != 0)
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
            if (N == 0)
            {
                ColorUtilities.BestMatch(StaticColorValidationData.Map, StaticColorValidationData.TestColors[i]);
            }
            else
            {
                m_octree.BestMatch(StaticColorValidationData.TestColors[i]);
            }
        }

        private ColorOctree m_octree;
    }
}