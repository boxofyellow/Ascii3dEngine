using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;


// From https://github.com/dotnet/BenchmarkDotNet/issues/466#issuecomment-326830110
public class TestFlagConfig : ManualConfig
{
    public TestFlagConfig()
    {
        // We need to include the the Clean;Build Targets to for a rebuild
        AddJob(Job.Default.WithRuntime(CoreRuntime.Core60).WithArguments(new MsBuildArgument[] { new("/p:TESTFLAG=true"), new("/t:Clean;Build") }).WithId("Test"));
        AddJob(Job.Default.WithRuntime(CoreRuntime.Core60).WithArguments(new MsBuildArgument[] { new("/t:Clean;Build") }).WithId("Test"));
    }
}
