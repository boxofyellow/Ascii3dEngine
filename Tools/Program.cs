
Console.WriteLine(@"
    Tools for computing the large boolean arrays in ColorUtilities
    To use 
      dotnet build -c Release --no-incremental -p:GENERATECOUNTS=true; dotnet run -c Release --no-build
    or
      dotnet build -c Release --no-incremental -p:PROFILECOLOR=true; dotnet run -c Release --no-build

    When you are done its a good idea to cleanup with 
      dotnet build -c Release --no-incremental
");

#if (PROFILECOLOR)
    AccuracyReport();
#endif

#if (GENERATECOUNTS)
    GenerateCounts();
#endif

#if (!GENERATECOUNTS && !GENERATECOUNTS)
    Console.WriteLine("Neither GENERATECOUNTS or PROFILECOLOR is set");
#endif

#pragma warning disable CS8321 // This method is only called with conditional compile time arguments 
static void GenerateCounts()
#pragma warning restore CS8321
{
    Console.WriteLine("Generating Counts");
    ColorUtilities.BruteForce.Counting();
}

#pragma warning disable CS8321 // This method is only called with conditional compile time arguments 
static void AccuracyReport()
#pragma warning restore CS8321
{
    Console.WriteLine("Generating Accuracy Report");
    ColorUtilities.BruteForce.AccuracyReport();
}