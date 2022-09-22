using CommandLine;

namespace Ascii3dEngine.TechDemo
{
    public class Settings
    {
        [Option('x', nameof(Axes))]
        public bool Axes { get; set; }

        [Option(nameof(AxesScale))]
        public double AxesScale { get; set; } = 1;

        [Option('c', nameof(Cube))]
        public bool Cube { get; set; }

        [Option(nameof(Spin))]
        public bool Spin { get; set; }

        [Option(nameof(MaxDegreeOfParallelism))]
        public int MaxDegreeOfParallelism { get; set; } = -1;

        [Option(nameof(LightSource))]
        public string? LightSource { get; set; }

        [Option(nameof(FloorHeight))]
        public double FloorHeight { get; set; } = -30;

        [Option(nameof(FloorScale))]
        public double FloorScale { get; set; } = 0.05;

        [Option(nameof(MaxFrameRate))]
        public int MaxFrameRate { get; set; } = 60;

        [Option(nameof(ModelFile))]
        public string? ModelFile { get; set; }

        [Option(nameof(ImagePlaneFile))]
        public string? ImagePlaneFile { get; set; }

        [Option(nameof(ImageScale))]
        public double ImageScale { get; set; } = 1;

        [Option(nameof(ImageSphereFile))]
        public string? ImageSphereFile { get; set; }

        [Option(nameof(ImageSphereRadius))]
        public double ImageSphereRadius { get; set; } = 1;

        [Option(nameof(To))]
        public string? To { get; set; }

        [Option(nameof(From))]
        public string? From { get; set; }

        [Option(nameof(Up))]
        public string? Up { get; set; }

        [Option(nameof(FontName))]
        public string? FontName { get; set; }

        public Point3D GetLightSource() => Point3D.Parse(LightSource, new (0, 200, 0));

        public Point3D GetTo() => Point3D.Parse(To, new(0, 0, 0));

        public Point3D GetFrom() => Point3D.Parse(From, new(50, 50, 50));

        public Point3D GetUp() => Point3D.Parse(Up, Point3D.YUnit);
    }
}