using CommandLine;

namespace Ascii3dEngine
{
    public class Settings
    {
        [Option('x', nameof(Axes))]
        public bool Axes { get; set; }

        [Option('m', nameof(MaxDegreeOfParallelism))]
        public int MaxDegreeOfParallelism { get; set; } = 16;

    }
}