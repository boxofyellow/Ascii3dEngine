using CommandLine;

class Program
{
    static private int Main(string[] args) => Parser.Default.ParseArguments<Settings>(args).MapResult(Run, HandleParseError);

    static private int HandleParseError(IEnumerable<Error> errs) => 100;

    private static int Run(Settings settings)
    {
        Console.WriteLine(@"
            Tools for computing character data that goes into CharMap.yaml
            It is a 3 step step process
            1. run `dotnet run`
               This generates text in the console
            2. Screen cap the output following the instructions
               Save the content to a log file
            3. run `dotnet build -c Release --no-incremental -p:GENERATECOUNTS=true; dotnet run -c Release --no-build --ChartImagePath {{ Path to file from Step 2 }}`
               This will generate CharMap.yaml

            Some good idea for follow up
            1. run `dotnet build -c Release --no-incremental`
            2. Replace the CharMap.yaml file under `Engine` to use that as the new default

        ");

#if (PROFILECOLOR)
        AccuracyReport();
#else
        if (string.IsNullOrEmpty(settings.ChartImagePath))
        {
            DrawCharChart(settings);
        }
        else
        {
            ProcessChartChart(settings);
        }
#endif

        return 0;
    }

    #pragma warning disable CS8321 // This method is only called with conditional compile time arguments 
    static void AccuracyReport()
    #pragma warning restore CS8321
    {
        Console.WriteLine("Generating Accuracy Report");
        ColorUtilities.BruteForce.AccuracyReport();
    }

    static void DrawCharChart(Settings settings)
    {
        var back = Console.BackgroundColor;
        var fore = Console.ForegroundColor;

        try
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("Copy content below this line make sure that some of the white box, but none of the black box gets cutout");
            Console.WriteLine("Make sure to include green boxes on the right, and the green bar at the bottom");
            Console.WriteLine("----------------------------");

            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write("   ");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" ");
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(" ");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" ");
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(" ");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" ");
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write("   ");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" ");
            Console.WriteLine();

            for (int color = (int)ConsoleColor.Black; color <= (int)ConsoleColor.White; color++)
            {
                Console.BackgroundColor = (ConsoleColor)color;
                Console.Write(" ");
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;

            var c = CharMap.MinChar; // Start at 32 for space;
            var itemsPerRow = settings.ItemsPerRow;
            while (c <= c_maxChar)
            {
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.WriteLine();
                Console.WriteLine();
                Console.Write(" ");
                Console.BackgroundColor = ConsoleColor.Black;
                for (var col = 0; col < itemsPerRow && c <= c_maxChar; col++, c++)
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.Write($"{c,4}:");
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.Write("(");
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.Write((char)c);
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.Write(")");
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.Write(" ");
                }

                Console.BackgroundColor = ConsoleColor.Green;
                Console.Write(" ");
            }

            Console.BackgroundColor = ConsoleColor.Blue;
            Console.WriteLine();

            Console.BackgroundColor = ConsoleColor.Green;
            Console.WriteLine("");

            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine();
            Console.WriteLine("----------------------------");
            Console.WriteLine("Copy content above this line");
            Console.WriteLine("Save the file in format that preserves all pixel data (so png or bmp, but not jpeg");
        }
        finally
        {
            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;
        }
    }

    private static void ProcessChartChart(Settings settings)
    {
        var computed = CharProcessor.ComputeCharCounts(settings.ChartImagePath, settings.ItemsPerRow);
        var mapWithoutStaticData = new CharMap(
            computed.Counts,
            computed.Width,
            computed.Height,
            backgroundsToSkip: new bool[0,0],
            foregroundsToSkip: new bool[0,0]);

        Console.WriteLine("Computing static skip content");
        var staticSkipData = ColorUtilities.BruteForce.ComputeStaticSkip(mapWithoutStaticData);

        var mapData = mapWithoutStaticData.ToCharMapData();
        mapData.BackgroundsToSkip = staticSkipData.BackgroundsToSkip;
        mapData.ForegroundsToSkip = staticSkipData.ForegroundsToSkip;

        Console.WriteLine($"Writing data to {Path.GetFullPath(settings.OutputFilePath)}");
        File.WriteAllText(settings.OutputFilePath, mapData.ToString());
    }

    private const int c_maxChar = (int)byte.MaxValue;
}