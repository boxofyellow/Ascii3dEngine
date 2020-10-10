using System.Collections.Generic;

namespace Ascii3dEngine
{
    public static class RayTracer
    {
        public static void Trace(bool[,] imageData, Scene scene, Projection projection, List<Actor> actors)
        {
            int[,] objects = FindObjects(scene.Screen.Size.H, scene.Screen.Size.V, scene, actors);
        }

        public static string[] TraceCharRay(int width, int height, Scene scene, CharMap map, List<Actor> actors)
        {
            int[,] objects = FindObjects(width, height, scene, actors);

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
        }

        private static int[,] FindObjects(int width, int height, Scene scene, List<Actor> actors)
        {
            int[,] result = new int[width, height];

            // we use half here because we span form -Size/2 to Size/2
            Point3D halfSide = scene.Camera.Side / 2;
            Point3D halfUp = scene.Camera.Up / 2;

            // These should be configurable, they represent the dimentions (and distance) that our Window in the real world cordites offset from Camera.From
            double windowWidth = 10.25 * Utilities.FudgeFactor;
            double windowHight = 10.25;
            double windowDistance = 10;

            Point3D forward = (scene.Camera.Direction.Normalized() * windowDistance)
                            + scene.Camera.From;  // We need to add this each one, so might as well add it now

            //  They should be selected so that the the box described as such is just visiable, it should just kiss the ourter edge
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

            foreach (Actor actor in actors)
            {
                actor.StartRayRender();
            }

            for (int x = default; x < result.GetLength(0); x++)
            {
                Point3D left = halfSide * (dx * (double)(x - midX)) + forward;
                for (int y = default; y < result.GetLength(1); y++)
                {
                    Point3D point = left + (halfUp * (dy * (double)(midY - y)));

                    // We now have two points (Camera.From) and this point we just computed
                    // Not that we need help computing this by here is the video about the parametric equations of a line passing through a point
                    // https://www.youtube.com/watch?v=QY15VEK9slo
                    Point3D vector = point - scene.Camera.From;

                    double minDistance = double.MaxValue;
                    int minId = default;

                    foreach (Actor actor in actors)
                    {
                        (double distance, int id) = actor.RenderRay(scene.Camera.From, vector);
                        if (id != default && distance < minDistance)
                        {
                            minId = id;
                            minDistance = distance;
                        }
                    }

                    result[x, y] = minId;
                }
            } 

            return result;
        }
    }
}