using CommandLine;

namespace Ascii3dEngine
{
    public class Settings
    {
        [Option('x', nameof(Axes))]
        public bool Axes { get; set; }

        [Option('c', nameof(Cube))]
        public bool Cube { get; set; }

        [Option(nameof(SpinCube))]
        public bool SpinCube { get; set; }

        [Option(nameof(HideBack))]
        public bool HideBack {get; set; }

        [Option(nameof(MaxDegreeOfParallelism))]
        public int MaxDegreeOfParallelism { get; set; } = -1;

        [Option(nameof(UseLineFitter))]
        public bool UseLineFitter { get; set; }

        [Option(nameof(MaxFrameRate))]
        public int MaxFrameRate { get; set; } = 60;
    }
}