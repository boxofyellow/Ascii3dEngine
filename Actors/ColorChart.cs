using System;
using System.Collections.Generic;
using SixLabors.ImageSharp.PixelFormats;

namespace Ascii3dEngine
{
    public class ColorChart : Actor
    {
        public ColorChart(Settings settings, CharMap map) : base()
        {
            m_map = map;

            double max = (double)(map.MaxX * map.MaxY);
            var colors = new HashSet<Rgb24>();
            foreach (ConsoleColor cc in ColorUtilities.ConsoleColors)
            {
                Rgb24 c = ColorUtilities.NamedColor(cc);
                foreach (ConsoleColor occ in ColorUtilities.ConsoleColors)
                {
                    Rgb24 oc = ColorUtilities.NamedColor(occ);
                    foreach (var count in map.Counts)
                    {
                        double uncovered = max - count.Count;
                        var nc = new Rgb24(
                            (byte)((int)(((double)((int)c.R) * (double)count.Count) + ((double)((int)oc.R) * uncovered))/max),
                            (byte)((int)(((double)((int)c.G) * (double)count.Count) + ((double)((int)oc.G) * uncovered))/max),
                            (byte)((int)(((double)((int)c.B) * (double)count.Count) + ((double)((int)oc.B) * uncovered))/max)
                        );
                        if (colors.Add(nc))
                        {
                            m_colors.Add(((char)count.Char, cc, occ, new Point3D(nc.R, nc.G, nc.B)));
                        }
                    }
                }
            }
        }

        public override void Render(Projection projection, bool[,] imageData, List<Label> labels)
        {
            foreach (var (character, foreground, background, point) in m_colors)
            {
                (bool inView, _, Point2D p2) = projection.Trans_Line(point, point);
                if (inView)
                {
                    labels.Add(new Label(
                        p2.H / m_map.MaxX,
                        p2.V / m_map.MaxY,
                        character,
                        foreground,
                        background));
                }
            }
        }

        public override Point3D NormalAt(Point3D intersection, int id) => throw new NotImplementedException("This should not be used with Ray");

        private readonly CharMap m_map;

        private readonly List<(char Character, ConsoleColor Foreground, ConsoleColor Background, Point3D Point)> m_colors = new();
    }
}