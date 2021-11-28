using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Ascii3dEngine.Benchmark
{
    /*
    From the last run
|        Method |     N |        Mean |     Error |    StdDev |
|-------------- |------ |------------:|----------:|----------:|
| FindAllColors |     0 |   191.73 ms |  3.734 ms |  3.493 ms |
| FindAllColors |     1 |   562.65 ms | 10.986 ms | 11.755 ms |
| FindAllColors |     2 |   321.95 ms |  6.420 ms |  6.593 ms |
| FindAllColors |     4 |   175.89 ms |  3.383 ms |  4.631 ms |
| FindAllColors |     8 |    99.05 ms |  1.737 ms |  1.625 ms |
| FindAllColors |    16 |    63.01 ms |  0.895 ms |  0.793 ms |
| FindAllColors |    32 |    45.11 ms |  0.733 ms |  0.686 ms |
| FindAllColors |    64 |    39.65 ms |  0.763 ms |  0.965 ms |
| FindAllColors |   128 |    38.08 ms |  0.730 ms |  0.781 ms |
| FindAllColors |   256 |    42.32 ms |  0.823 ms |  0.880 ms |
| FindAllColors |   512 |    55.81 ms |  1.049 ms |  0.982 ms |
| FindAllColors |  1024 |    69.03 ms |  1.361 ms |  1.398 ms |
| FindAllColors |  2048 |   254.36 ms |  4.866 ms |  5.976 ms |
| FindAllColors |  4096 |   319.60 ms |  6.283 ms |  7.236 ms |
| FindAllColors |  8192 |   318.77 ms |  6.369 ms |  6.540 ms |
| FindAllColors | 16384 | 2,265.59 ms | 14.466 ms | 13.531 ms |
    */

    //[Config(typeof(TestFlagConfig))]
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
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
                    m_octree!.BestMatch(StaticColorValidationData.TestColors[i]);
                }
            }
        }

        private ColorOctree? m_octree;
    }
}