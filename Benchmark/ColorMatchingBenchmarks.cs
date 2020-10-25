using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Ascii3dEngine.Benchmark
{
    /*
    From the last run
|        Method |     N |        Mean |     Error |    StdDev |
|-------------- |------ |------------:|----------:|----------:|
| FindAllColors |     0 |   310.19 ms |  6.088 ms |  6.766 ms |
| FindAllColors |     1 |   509.35 ms | 10.151 ms | 16.391 ms |
| FindAllColors |     2 |   316.57 ms |  6.284 ms | 10.499 ms |
| FindAllColors |     4 |   177.63 ms |  3.530 ms |  4.589 ms |
| FindAllColors |     8 |   101.06 ms |  1.953 ms |  2.470 ms |
| FindAllColors |    16 |    63.03 ms |  0.758 ms |  0.633 ms |
| FindAllColors |    32 |    46.39 ms |  0.924 ms |  1.100 ms |
| FindAllColors |    64 |    40.66 ms |  0.782 ms |  0.989 ms |
| FindAllColors |   128 |    39.21 ms |  0.778 ms |  1.091 ms |
| FindAllColors |   256 |    44.05 ms |  0.864 ms |  1.124 ms |
| FindAllColors |   512 |    57.87 ms |  1.128 ms |  1.207 ms |
| FindAllColors |  1024 |    70.62 ms |  1.369 ms |  1.576 ms |
| FindAllColors |  2048 |   261.44 ms |  5.129 ms |  7.356 ms |
| FindAllColors |  4096 |   316.34 ms |  5.878 ms |  4.908 ms |
| FindAllColors |  8192 |   315.26 ms |  4.776 ms |  4.233 ms |
| FindAllColors | 16384 | 2,291.39 ms | 16.818 ms | 15.731 ms |
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

    // Might be handy - https://github.com/dotnet/BenchmarkDotNet/issues/466#issuecomment-326830110
}