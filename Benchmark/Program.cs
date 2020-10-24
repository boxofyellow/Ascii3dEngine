using BenchmarkDotNet.Running;

namespace Ascii3dEngine.Benchmark
{
    class Program
    {
        static void Main(string[] args) => BenchmarkRunner.Run(typeof(Program).Assembly);
    }
}
