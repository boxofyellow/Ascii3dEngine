using CommandLine;

public class Settings
{
    [Option('i', nameof(ChartImagePath))]
    public string ChartImagePath { get; set; } = string.Empty;

    [Option('r', nameof(ItemsPerRow))]
    public int ItemsPerRow { get; set; } = 16;

    [Option('o', nameof(OutputFilePath))]
    public string OutputFilePath { get; set; } = Path.GetFileName(CharMap.DefaultMapFilePath);

    [Option('c', nameof(ColorChart))]
    public bool ColorChart { get; set; }

    [Option('u', nameof(UseComputedColors))]
    public bool UseComputedColors { get; set; }
}