using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace Ascii3dEngine.Benchmark
{
    // From https://github.com/dotnet/BenchmarkDotNet/issues/466#issuecomment-326830110
    public class TestFlagConfig : ManualConfig
    {
        public TestFlagConfig()
        {
            // We need to include the the Clean;Build Targets to for a rebuild
            AddJob(Job.Default.WithRuntime(CoreRuntime.Core31).WithArguments(new[] { new MsBuildArgument("/p:TESTFLAG=true"), new MsBuildArgument("/t:Clean;Build") }).WithId("Test"));
            AddJob(Job.Default.WithRuntime(CoreRuntime.Core31).WithArguments(new[] { new MsBuildArgument("/t:Clean;Build") }).WithId("Test"));
        }
    }
}