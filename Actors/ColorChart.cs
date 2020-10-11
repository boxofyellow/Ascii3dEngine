using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace  Ascii3dEngine
{
    public class ColorChart : Actor
    {
        public ColorChart(Settings settings, CharMap map) : base()
        {
            m_map = map;

            var namedColors = typeof(Color)
                .GetFields(BindingFlags.Public | BindingFlags.Static)               // Get all the public Static Fields
                .Where(f => f.FieldType == typeof(Color) && f.IsInitOnly)           // We only want the Readonly Color ones
                .ToDictionary(f => f.Name,                                          // Map their Name to the value in a Dictionary that ignores case
                              f => ((Color)f.GetValue(default)).ToPixel<Rgb24>(),   // We want the RGB values
                              StringComparer.OrdinalIgnoreCase);

            // it looks like they have don't have Dark Yellow, so just throw Dark Goldenrod in there...
            // With out this we find like 10147, with a max difference of 8, with it we find 11771 (and addition of like 16%) and max difference of 7 (and a reduction of like 13%)
            namedColors.Add(ConsoleColor.DarkYellow.ToString(), Color.DarkGoldenrod.ToPixel<Rgb24>());

            var consoleColors = Enum.GetValues(typeof(ConsoleColor)).OfType<ConsoleColor>().ToList();
            double max = (double)(map.MaxX * map.MaxY);
            var colors = new HashSet<Rgb24>();
            foreach (ConsoleColor cc in consoleColors)
            {
                if (namedColors.TryGetValue(cc.ToString(), out var c))
                {
                    foreach (ConsoleColor occ in consoleColors)
                    {
                        if (namedColors.TryGetValue(occ.ToString(), out var oc))
                        {
                            foreach (var count in map.m_counts)
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
                else
                {
                    Console.WriteLine($"Did not find {cc}");
                }
            }
        }

        public override void Render(Projection projection, bool[,] imageData, List<Label> labels)
        {
            foreach (var color in m_colors)
            {
                (bool inView, _, Point2D p2) = projection.Trans_Line(color.Point, color.Point);
                if (inView)
                {
                    labels.Add(new Label(
                        p2.H / m_map.MaxX,
                        p2.V / m_map.MaxY,
                        color.Character,
                        color.Foreground,
                        color.Background));
                }
            }
        }

        private readonly CharMap m_map;

        private readonly List<(Char Character, ConsoleColor Foreground, ConsoleColor Background, Point3D Point)> m_colors 
            = new List<(Char Character, ConsoleColor Foreground, ConsoleColor Background, Point3D Point)>();
    }
}