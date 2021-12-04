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
            FindObjects(settings, scene.Screen.Size.H, scene.Screen.Size.V, scene, actors, sources: Array.Empty<LightSource>());
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
            => FindObjects(settings, width, height, scene, actors, sources.ToArray());

        private static Rgb24[,] FindObjects(Settings settings, int width, int height, Scene scene, List<Actor> actors, LightSource[] sources)
        {
            const double maxColorValue = byte.MaxValue;

            var result = new Rgb24[width, height];
            if (s_lastRunCache == null || s_lastRunCache.GetLength(0) != width || s_lastRunCache.GetLength(1) != height)
            {
                s_lastRunCache = new LastRunCell[width, height];
            }

            // we use half here because we span form -Size/2 to Size/2
            var halfSide = scene.Camera.Right / 2;
            var halfUp = scene.Camera.Up / 2;

            // These should be configurable, they represent the dimentions (and distance) that our Window in the real world cordites offset from Camera.From
            double windowWidth = 10.25 * Utilities.FudgeFactor;
            double windowHight = 10.25;
            double windowDistance = 10;

            var forward = (scene.Camera.Direction.Normalized() * windowDistance)
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

            var options = new ParallelOptions() { MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism };

            Parallel.ForEach(
                source: actors,
                options,
                (actor) => actor.StartRayRender(scene.Camera.From, sources));

            Parallel.For(
                fromInclusive: default,
                toExclusive: result.GetLength(1),
                options,
                (y) =>
            {
                // Using midY - y here because we want the top row to correspond with result[x][0]
                var rowStart = forward + (halfUp * (dy * (double)(midY - y)));

                for (int x = default; x < result.GetLength(0); x++)
                {
#if (DEBUG)
                    if (DebugUtilities.DebugPoint(x, y))
                    {
                        // just something so we can add break point
                    }
#endif

                    // using x - midX here because we want the left column to correspond with result[0][y]
                    var point = rowStart + halfSide * (dx * (double)(x - midX));

                    // We now have two points (Camera.From) and this point we just computed
                    // Not that we need help computing this, but here is the video about the parametric equations of a line passing through a point
                    // https://www.youtube.com/watch?v=QY15VEK9slo
                    var vector = point - scene.Camera.From;

                    double minDistanceProxy = double.MaxValue;
                    int minId = default;
                    Point3D minIntersection = default;
                    Actor? minActor = default;

                    // Right now this is only getting us a better starting point for minDistanceProxy
                    // But there are other times when this should be handy. Namely if this actor (and the camera) have not moved
                    // In that case we should not need to check all the faces of this actor.
                    var last = s_lastRunCache[x, y];
                    if (last.LastActor != default)
                    {
                        (double distanceProxy, bool hit, var intersection) = last.LastActor.RenderRayForId(last.LastId, scene.Camera.From, vector);
                        if (hit)
                        {
                            minId = last.LastId;
                            minDistanceProxy = distanceProxy;
                            minIntersection = intersection;
                            minActor = last.LastActor;
                        }
                    }

                    foreach (var actor in actors)
                    {
                        (double distanceProxy, int id, var intersection) = actor.RenderRay(scene.Camera.From, vector, minDistanceProxy);
                        if (id != default && distanceProxy < minDistanceProxy)
                        {
                            minId = id;
                            minDistanceProxy = distanceProxy;
                            minIntersection = intersection;
                            minActor = actor;
                        }
                    }

                    if (minActor != default)
                    {
                        s_lastRunCache[x, y] = new(minActor, minId);
#if (DEBUG)
                        DebugUtilities.UpdateTrackingTarget(x, y, minActor, minId);
                        if (DebugUtilities.DisplayMark(x, y, minActor, minId))
                        {
                            result[x, y] = DebugUtilities.Color;
                            continue;
                        }
#endif

                        // See Page 414
                        var properties = minActor.ColorAt(minIntersection, minId);
                        var m = minActor.NormalAt(minIntersection, minId);
                        var v = scene.Camera.From - point; //from P to eye
                        double mLength = m.Length;

                        bool doubleSided = minActor.DoubleSided(minIntersection, minId);

                        // just for testing lets assume all light intensity are 0.5
                        // See Page 761
                        double red = 0.5 * properties.Ambient.X;
                        double green = 0.5 * properties.Ambient.Y;
                        double blue = 0.5 * properties.Ambient.Z;

                        for (int i = 0; i < sources.Length; i++)
                        {
                            var source = sources[i];

                            // s P to light source
                            var lightVector = minIntersection - source.Point;

                            bool inShadow = false;
                            foreach (var actor in actors)
                            {
                                if (actor.DoesItCastShadow(i, source.Point, lightVector, minId))
                                {
                                    inShadow = true;
                                    break;
                                }
                            }

                            if (!inShadow)
                            {
                                double redSource = (double)source.Color.R / maxColorValue;
                                double greenSource = (double)source.Color.G / maxColorValue;
                                double blueSource = (double)source.Color.B / maxColorValue; 

                                // Page 418 Halfway vector
                                var h = lightVector + v;

                                // we should compute Id = max(0, (s dot m) / (|s||m|))
                                double iDiffuse = lightVector.DotProduct(m);

                                if (doubleSided || iDiffuse < 0)
                                {
                                    if (iDiffuse < 0)
                                    {
                                        iDiffuse *= -1;
                                    }
                                    iDiffuse /= lightVector.Length * mLength;
                                    red += iDiffuse * redSource * properties.Diffuse.X;
                                    green += iDiffuse * greenSource * properties.Diffuse.Y;
                                    blue += iDiffuse * blueSource * properties.Diffuse.Z;
                                
                                }

                                // we should compute phong = max(0, (h dot m) / (|h||m|))
                                // Don't forget ^Shininess
                                double phong = h.DotProduct(m);
                                if (doubleSided || phong < 0)
                                {
                                    if (phong < 0)
                                    {
                                        phong *= -1;
                                    }
                                    phong /= h.Length * mLength;
                                    if (phong != 1.0 && properties.Shininess != 1.0)
                                    {
                                        phong = Math.Pow(phong, properties.Shininess);
                                    }
                                    red += phong * redSource * properties.Specular.X;
                                    green += phong * greenSource * properties.Specular.Y;
                                    blue += phong * blueSource * properties.Specular.Z;
                                }
                            }
                        }

                        red = Math.Min(1.0, red);
                        green = Math.Min(1.0, green);
                        blue = Math.Min(1.0, blue);
                        result[x, y] = new((byte)(red * maxColorValue), (byte)(green * maxColorValue), (byte)(blue * maxColorValue));
                    }
                }
            });

            return result;
        }

        private static LastRunCell[,]? s_lastRunCache;

        private readonly struct LastRunCell
        {
            public LastRunCell(Actor? actor, int lastIndex)
            {
                LastActor = actor;
                LastId = lastIndex;
            }
            public readonly Actor? LastActor;
            public readonly int LastId;
        }
    }
}