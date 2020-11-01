using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Ascii3dEngine.Benchmark
{
    /*
    From the last run
|        Method |     N |        Mean |     Error |    StdDev |      Median |
|-------------- |------ |------------:|----------:|----------:|------------:|
| FindAllColors |     0 |   196.18 ms |  3.905 ms |  4.498 ms |   195.47 ms |
| FindAllColors |     1 |   589.18 ms | 10.923 ms | 21.046 ms |   582.05 ms |
| FindAllColors |     2 |   340.02 ms |  6.789 ms | 13.241 ms |   334.19 ms |
| FindAllColors |     4 |   184.33 ms |  3.616 ms |  4.020 ms |   182.73 ms |
| FindAllColors |     8 |   106.23 ms |  2.090 ms |  2.488 ms |   105.62 ms |
| FindAllColors |    16 |    67.42 ms |  1.313 ms |  1.348 ms |    67.45 ms |
| FindAllColors |    32 |    48.46 ms |  0.953 ms |  1.135 ms |    48.20 ms |
| FindAllColors |    64 |    42.48 ms |  0.699 ms |  0.654 ms |    42.39 ms |
| FindAllColors |   128 |    39.46 ms |  0.728 ms |  0.681 ms |    39.46 ms |
| FindAllColors |   256 |    45.92 ms |  0.792 ms |  1.002 ms |    45.74 ms |
| FindAllColors |   512 |    61.64 ms |  1.227 ms |  1.363 ms |    61.34 ms |
| FindAllColors |  1024 |    73.38 ms |  0.765 ms |  0.598 ms |    73.24 ms |
| FindAllColors |  2048 |   273.96 ms |  4.002 ms |  3.342 ms |   273.30 ms |
| FindAllColors |  4096 |   335.37 ms |  5.208 ms |  4.066 ms |   336.39 ms |
| FindAllColors |  8192 |   338.09 ms |  6.420 ms |  7.885 ms |   337.40 ms |
| FindAllColors | 16384 | 2,426.55 ms | 19.849 ms | 17.596 ms | 2,424.54 ms |
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