using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

    /*
    From the last run
|        Method |     N |        Mean |     Error |    StdDev |
|-------------- |------ |------------:|----------:|----------:|
| FindAllColors |     0 |   165.26 ms |  3.274 ms |  5.000 ms |
| FindAllColors |     1 |   470.36 ms |  9.393 ms | 20.018 ms |
| FindAllColors |     2 |   254.16 ms |  5.057 ms |  6.210 ms |
| FindAllColors |     4 |   132.74 ms |  2.627 ms |  2.811 ms |
| FindAllColors |     8 |    79.57 ms |  1.589 ms |  1.767 ms |
| FindAllColors |    16 |    52.70 ms |  1.025 ms |  1.097 ms |
| FindAllColors |    32 |    40.56 ms |  0.754 ms |  0.630 ms |
| FindAllColors |    64 |    35.03 ms |  0.617 ms |  0.577 ms |
| FindAllColors |   128 |    32.92 ms |  0.654 ms |  0.979 ms |
| FindAllColors |   256 |    33.15 ms |  0.629 ms |  0.588 ms |
| FindAllColors |   512 |    39.23 ms |  0.778 ms |  0.984 ms |
| FindAllColors |  1024 |    62.35 ms |  1.199 ms |  1.558 ms |
| FindAllColors |  2048 |   250.62 ms |  4.719 ms |  4.414 ms |
| FindAllColors |  4096 |   296.31 ms |  5.893 ms |  6.787 ms |
| FindAllColors |  8192 |   298.59 ms |  5.770 ms |  7.703 ms |
| FindAllColors | 16384 | 2,136.48 ms | 12.316 ms | 10.918 ms |
    */

//[Config(typeof(TestFlagConfig))]
[SimpleJob(RuntimeMoniker.Net60)]
[MarkdownExporter, AsciiDocExporter, HtmlExporter, CsvExporter, RPlotExporter]
public class ColorMatchingBenchmarks
{
    //[Params(0)]
    [Params(0, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        // This is really here to make sure we get this static info loaded before running our test
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