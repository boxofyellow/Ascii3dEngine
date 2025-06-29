using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

    /*
    From the last run
| Method        | N     | Mean        | Error     | StdDev    |
|-------------- |------ |------------:|----------:|----------:|
| FindAllColors | 0     |   133.64 ms |  2.254 ms |  2.108 ms |
| FindAllColors | 1     |   750.81 ms | 10.380 ms |  9.710 ms |
| FindAllColors | 2     |   529.10 ms | 10.528 ms | 10.811 ms |
| FindAllColors | 4     |   323.14 ms |  6.200 ms |  6.089 ms |
| FindAllColors | 8     |   189.37 ms |  3.391 ms |  3.172 ms |
| FindAllColors | 16    |   110.86 ms |  1.993 ms |  1.864 ms |
| FindAllColors | 32    |    72.78 ms |  1.387 ms |  1.542 ms |
| FindAllColors | 64    |    57.80 ms |  0.855 ms |  0.800 ms |
| FindAllColors | 128   |    51.92 ms |  1.035 ms |  0.968 ms |
| FindAllColors | 256   |    53.52 ms |  1.051 ms |  0.983 ms |
| FindAllColors | 512   |    64.28 ms |  0.775 ms |  0.725 ms |
| FindAllColors | 1024  |    85.67 ms |  0.820 ms |  0.767 ms |
| FindAllColors | 2048  |   171.66 ms |  1.699 ms |  1.506 ms |
| FindAllColors | 4096  |   305.40 ms |  4.229 ms |  3.956 ms |
| FindAllColors | 8192  |   275.24 ms |  3.330 ms |  3.114 ms |
| FindAllColors | 16384 | 2,062.96 ms |  9.769 ms |  8.660 ms |
    */

//[Config(typeof(TestFlagConfig))]
[SimpleJob(RuntimeMoniker.Net80)]
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