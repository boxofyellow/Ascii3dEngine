using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

    /*
    From the last run
|        Method |     N |        Mean |     Error |    StdDev |
|-------------- |------ |------------:|----------:|----------:|
| FindAllColors |     0 |   191.93 ms |  2.062 ms |  1.610 ms |
| FindAllColors |     1 |   579.70 ms | 11.426 ms | 14.857 ms |
| FindAllColors |     2 |   337.25 ms |  4.741 ms |  4.435 ms |
| FindAllColors |     4 |   189.31 ms |  3.737 ms |  4.154 ms |
| FindAllColors |     8 |   109.26 ms |  1.929 ms |  1.804 ms |
| FindAllColors |    16 |    65.20 ms |  1.293 ms |  1.489 ms |
| FindAllColors |    32 |    45.78 ms |  0.523 ms |  0.437 ms |
| FindAllColors |    64 |    40.78 ms |  0.725 ms |  0.679 ms |
| FindAllColors |   128 |    39.77 ms |  0.788 ms |  0.774 ms |
| FindAllColors |   256 |    42.79 ms |  0.682 ms |  0.638 ms |
| FindAllColors |   512 |    56.37 ms |  1.093 ms |  1.122 ms |
| FindAllColors |  1024 |    70.69 ms |  1.340 ms |  1.253 ms |
| FindAllColors |  2048 |   267.68 ms |  5.319 ms |  5.912 ms |
| FindAllColors |  4096 |   327.84 ms |  6.506 ms |  8.228 ms |
| FindAllColors |  8192 |   327.71 ms |  6.127 ms |  6.292 ms |
| FindAllColors | 16384 | 2,414.68 ms | 17.545 ms | 16.412 ms |
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