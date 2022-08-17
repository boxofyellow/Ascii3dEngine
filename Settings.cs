using CommandLine;

namespace Ascii3dEngine
{
    public class Settings
    {
        [Option('x', nameof(Axes))]
        public bool Axes { get; set; }

        [Option('c', nameof(Cube))]
        public bool Cube { get; set; }

        [Option(nameof(Spin))]
        public bool Spin { get; set; }

        [Option(nameof(MaxDegreeOfParallelism))]
        public int MaxDegreeOfParallelism { get; set; } = -1;

        [Option(nameof(MaxFrameRate))]
        public int MaxFrameRate { get; set; } = 60;

        [Option(nameof(ModelFile))]
        public string? ModelFile {get; set; }

        [Option(nameof(To))]
        public string? To {get; set;}

        [Option(nameof(From))]
        public string? From {get; set;}

        [Option(nameof(Up))]
        public string? Up {get; set;}

        [Option(nameof(PruneMap))]
        public bool PruneMap {get; set;}

        [Option(nameof(FontName))]
        public string? FontName { get; set;}

        public Point3D GetTo() => Point3D.Parse(To, new(0, 0, 0));

        public Point3D GetFrom() => Point3D.Parse(From, new(50, 50, 50));

        public Point3D GetUp() => Point3D.Parse(Up, new(0, 1, 0));
    }
}