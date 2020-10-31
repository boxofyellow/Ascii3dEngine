using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Ascii3dEngine.Benchmark
{
    /*
    From the last run
|        Method |     N |        Mean |     Error |    StdDev |      Median |
|-------------- |------ |------------:|----------:|----------:|------------:|
| FindAllColors |     0 |   325.29 ms |  6.437 ms |  6.611 ms |   326.04 ms |
| FindAllColors |     1 |   574.28 ms | 11.256 ms | 18.494 ms |   571.74 ms |
| FindAllColors |     2 |   327.58 ms |  6.449 ms | 11.629 ms |   324.44 ms |
| FindAllColors |     4 |   178.51 ms |  3.544 ms |  5.082 ms |   177.51 ms |
| FindAllColors |     8 |   104.71 ms |  2.055 ms |  2.446 ms |   103.66 ms |
| FindAllColors |    16 |    67.01 ms |  1.301 ms |  1.824 ms |    66.68 ms |
| FindAllColors |    32 |    48.30 ms |  0.956 ms |  1.371 ms |    47.64 ms |
| FindAllColors |    64 |    41.93 ms |  0.817 ms |  0.839 ms |    41.56 ms |
| FindAllColors |   128 |    38.95 ms |  0.772 ms |  0.919 ms |    38.63 ms |
| FindAllColors |   256 |    45.31 ms |  0.885 ms |  1.119 ms |    44.65 ms |
| FindAllColors |   512 |    60.13 ms |  1.186 ms |  1.456 ms |    59.80 ms |
| FindAllColors |  1024 |    73.81 ms |  1.454 ms |  1.839 ms |    72.98 ms |
| FindAllColors |  2048 |   267.54 ms |  5.259 ms |  7.372 ms |   265.24 ms |
| FindAllColors |  4096 |   335.45 ms |  6.640 ms |  7.647 ms |   335.72 ms |
| FindAllColors |  8192 |   332.54 ms |  6.495 ms |  7.732 ms |   329.58 ms |
| FindAllColors | 16384 | 2,411.56 ms | 20.148 ms | 17.861 ms | 2,415.96 ms |
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