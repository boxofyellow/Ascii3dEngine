using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SixLabors.ImageSharp.PixelFormats;

namespace Ascii3dEngine
{
    public static class RayTracer
    {
        public static void Trace(Settings settings, bool[,] imageData, Scene scene, Projection projection, List<Actor> actors)
        {
            FindObjects(settings, scene.Screen.Size.H, scene.Screen.Size.V, scene, actors, sources: new LightSource[0]);
        }

        public static string[] TraceCharRay(Settings settings, int width, int height, Scene scene, CharMap map, List<Actor> actors)
        {
            throw new NotImplementedException("Um do we need this any more...");
            /*
            int[,] objects = FindObjects(settings, width, height, scene, actors, sources: new LightSource[0]);

            string[] lines = new string[height];
            for (int y = default; y < height; y++)
            {
                char[] line = new char[width];
                for (int x = default; x < width; x++)
                {
                    int id = objects[x,y];
                    line[x] = id == 0 ? ' ' : map.GetUniqueChar(id);
                }
                lines[y] = new string(line);
            }
            return lines;
            */
        }

        public static Rgb24[,] TraceColor(Settings settings, int width, int height, Scene scene, CharMap map, List<Actor> actors, List<LightSource> sources)
        {
            return FindObjects(settings, width, height, scene, actors, sources.ToArray());
        }

        private static Rgb24[,] FindObjects(Settings settings, int width, int height, Scene scene, List<Actor> actors, LightSource[] sources)
        {
            Rgb24[,] result = new Rgb24[width, height];

            // we use half here because we span form -Size/2 to Size/2
            Point3D halfSide = scene.Camera.Right / 2;
            Point3D halfUp = scene.Camera.Up / 2;

            // These should be configurable, they represent the dimentions (and distance) that our Window in the real world cordites offset from Camera.From
            double windowWidth = 10.25 * Utilities.FudgeFactor;
            double windowHight = 10.25;
            double windowDistance = 10;

            Point3D forward = (scene.Camera.Direction.Normalized() * windowDistance)
                            + scene.Camera.From;  // We need to add this each one, so might as well add it now

            //  They should be selected so that the they box described as such is just visiable, it should just kiss the ourter edge
            // Point3D p1 = (halfSide * -windowWidth/2.0) + forward + (halfUp * windowHight/2.0);
            // Point3D p2 = (halfSide * windowWidth/2.0) + forward + (halfUp * windowHight/2.0);
            // Point3D p3 = (halfSide * windowWidth/2.0) + forward + (halfUp * -windowHight/2.0);
            // Point3D p4 = (halfSide * -windowWidth/2.0) + forward + (halfUp * -windowHight/2.0);
            // imageData.DrawLine(projection, p1, p2);
            // imageData.DrawLine(projection, p2, p3);
            // imageData.DrawLine(projection, p3, p4);
            // imageData.DrawLine(projection, p4, p1);

            double dx = windowWidth / (double)result.GetLength(0);
            double dy = windowHight / (double)result.GetLength(1);
            int midX = result.GetLength(0) / 2;
            int midY = result.GetLength(1) / 2;

            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism };

            Parallel.ForEach(
                source: actors,
                options,
                (actor) => actor.StartRayRender(scene.Camera.From, sources));

            Parallel.For(
                fromInclusive: default,
                toExclusive: result.GetLength(1),
                new ParallelOptions { MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism },
                (y) =>
            {
                // Using midY - y here because we want the top row to correspond with result[x][0]
                Point3D rowStart = forward + (halfUp * (dy * (double)(midY - y)));

                for (int x = default; x < result.GetLength(0); x++)
                {
#if (DEBUG)
                    if (DebugUtilities.DebugPoint(x, y))
                    {
                        // just something so we can add break point
                    }
#endif

                    // using x - midX here because we want the left column to correspond with result[0][y]
                    Point3D point = rowStart + halfSide * (dx * (double)(x - midX));

                    // We now have two points (Camera.From) and this point we just computed
                    // Not that we need help computing this by here is the video about the parametric equations of a line passing through a point
                    // https://www.youtube.com/watch?v=QY15VEK9slo
                    Point3D vector = point - scene.Camera.From;

                    double minDistanceProxy = double.MaxValue;
                    int minId = default;
                    Point3D minIntersection = default;
                    Actor minActor = default;

                    foreach (Actor actor in actors)
                    {
                        (double distanceProxy, int id, Point3D intersection) = actor.RenderRay(scene.Camera.From, vector, minDistanceProxy);
                        if (id != default && distanceProxy < minDistanceProxy)
                        {
                            minId = id;
                            minDistanceProxy = distanceProxy;
                            minIntersection = intersection;
                            minActor = actor;
                        }
                    }

                    if (minId != default)
                    {
#if (DEBUG)
                        DebugUtilities.UpdateTrackingTarget(x, y, minActor, minId);
                        if (DebugUtilities.DisplayMark(x, y, minActor, minId))
                        {
                            result[x, y] = DebugUtilities.Color;
                            continue;
                        }
#endif

                        ColorProperties properties = minActor.ColorAt(minIntersection, minId);
                        Point3D m = minActor.NormalAt(minIntersection, minId);

                        // just for testing lets assume all light intensity are 1.0

                        // See Page 761
                        double red = 1.0 * properties.Ambient.X;
                        double green = 1.0 * properties.Ambient.Y;
                        double blue = 1.0 * properties.Ambient.Z;

                        // we should compute Iambert = max(0, (s dot m) / (|s||m|))
                        red += (0.7 * properties.Diffuse.X);
                        green += (0.7 * properties.Diffuse.Y);
                        blue += (0.7 * properties.Diffuse.Z);

                        for (int i = 0; i < sources.Length; i++)
                        {
                            LightSource source = sources[i];
                            Point3D lightVector = minIntersection - source.Point;

                            bool inShadow = false;
                            foreach (Actor actor in actors)
                            {
                                if (actor.DoesItCastShadow(i, source.Point, lightVector, minId))
                                {
                                    inShadow = true;
                                    break;
                                }
                            }

                            if (!inShadow)
                            {
                                // we should compute phong = max(0, (h dot m) / (|h||m|))
                                // Don't for get ^Shininess
                                red += (0.7 * properties.Specular.X);
                                green += (0.7 * properties.Specular.Y);
                                blue += (0.7 * properties.Specular.Z);
                            }
                        }

                        red = Math.Min(1.0, red);
                        green = Math.Min(1.0, green);
                        blue = Math.Min(1.0, blue);
                        result[x, y] = new Rgb24((byte)(red * 255.0), (byte)(green * 255.0), (byte)(blue * 255.0));
                    }
                }
            });

            return result;
        }
    }
}