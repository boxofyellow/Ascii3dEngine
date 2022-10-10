using CommandLine;

class Program
{
    static private int Main(string[] args) => Parser.Default.ParseArguments<Settings>(args).MapResult(Run, HandleParseError);

    static private int HandleParseError(IEnumerable<Error> errs) => 100;

    private static int Run(Settings settings)
    {
        Console.WriteLine(@"
            Tools for computing character data and the large boolean arrays in ColorUtilities
            To use
            - Generate the text version of the map that can be screen capped to process next
              - dotnet run
            - Process the screen capture to generate the CharMap.yaml file
              - dotnet run -- -i {{ Path to the image file }}
            - To compute the contests of the arrays in ColorUtilities 
              - dotnet build -c Release --no-incremental -p:GENERATECOUNTS=true; dotnet run -c Release --no-build
            - To profile that process
              - dotnet build -c Release --no-incremental -p:PROFILECOLOR=true -p:GENERATECOUNTS=true; dotnet run -c Release --no-build

            After using either compile file  its a good idea to cleanup with 
            - dotnet build -c Release --no-incremental
        ");

#if (PROFILECOLOR)
        AccuracyReport();
#endif

#if (GENERATECOUNTS)
        GenerateCounts();
#endif

#if (!GENERATECOUNTS && !GENERATECOUNTS)
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
        var map = new CharMap(computed.Counts, computed.Width, computed.Height);
        Console.WriteLine($"Writing data to {Path.GetFullPath(settings.OutputFilePath)}");
        File.WriteAllText(settings.OutputFilePath, map.ToString());
    }

    private const int c_maxChar = (int)byte.MaxValue;
}