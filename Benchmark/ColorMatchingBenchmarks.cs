using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Ascii3dEngine.Benchmark
{
    /*
    From the last run
|        Method |     N |        Mean |     Error |    StdDev |
|-------------- |------ |------------:|----------:|----------:|
| FindAllColors |    -1 |   344.31 ms |  6.701 ms |  9.172 ms |
| FindAllColors |     0 |   347.18 ms |  5.688 ms |  6.322 ms |
| FindAllColors |     1 |   565.51 ms | 11.137 ms |  9.300 ms |
| FindAllColors |     2 |   327.22 ms |  6.464 ms | 11.149 ms |
| FindAllColors |     4 |   176.61 ms |  3.424 ms |  3.035 ms |
| FindAllColors |     8 |   101.25 ms |  2.007 ms |  2.230 ms |
| FindAllColors |    16 |    67.52 ms |  1.265 ms |  1.353 ms |
| FindAllColors |    32 |    47.56 ms |  0.941 ms |  1.120 ms |
| FindAllColors |    64 |    41.88 ms |  0.717 ms |  0.767 ms |
| FindAllColors |   128 |    40.47 ms |  0.796 ms |  1.034 ms |
| FindAllColors |   256 |    45.31 ms |  0.901 ms |  0.925 ms |
| FindAllColors |   512 |    58.55 ms |  1.135 ms |  1.214 ms |
| FindAllColors |  1024 |    73.14 ms |  1.390 ms |  1.601 ms |
| FindAllColors |  2048 |   271.94 ms |  5.274 ms |  7.393 ms |
| FindAllColors |  4096 |   330.59 ms |  6.146 ms |  5.749 ms |
| FindAllColors |  8192 |   332.82 ms |  6.560 ms |  6.736 ms |
| FindAllColors | 16384 | 2,436.27 ms | 13.614 ms | 11.368 ms |
    */
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [MarkdownExporter, AsciiDocExporter, HtmlExporter, CsvExporter, RPlotExporter]
    public class Benchmarks
    {
        [Params(-1, 0, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384)]
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
                if (N == -1)
                {
                    ColorUtilities.BestMatch(StaticColorValidationData.Map, StaticColorValidationData.TestColors[i], testFlag: true);
                }
                else if (N == 0)
                {
                    ColorUtilities.BestMatch(StaticColorValidationData.Map, StaticColorValidationData.TestColors[i], testFlag: false);
                }
                else
                {
                    m_octree.BestMatch(StaticColorValidationData.TestColors[i]);
                }
            }
        }

        private ColorOctree m_octree;
    }

    // Might be handy - https://github.com/dotnet/BenchmarkDotNet/issues/466#issuecomment-326830110
}