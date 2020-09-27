namespace Ascii3dEngine
{
    public static class RayTracer
    {
        public static void Trace(bool[,] imageData, Scene scene, Projection projection)
        {
            int[,] objects = new int[scene.Screen.Size.H, scene.Screen.Size.V];

            //
            // So it looks like we have some scalding problems that I need to address
            // Really this should be /2 not /20 *100 is really big, and the box looks more like a rectangle
            // Does this have anything to do with the mystory *1.75? 
            // 

            int midX = objects.GetLength(0) / 20;
            int midY = objects.GetLength(1) / 20;

            // we use half here because we span form -Size/2 to Size/2
            Point3D halfSide = scene.Camera.Side / 2;
            Point3D halfUp = scene.Camera.Up / 2;

            Point3D forward = (scene.Camera.Direction.Normalized() * 100) // This 10 should be adjustable 
                            + scene.Camera.From;  // We need to add this each one, so might as well add it now  

            /*
            for (int x = default; x < objects.GetLength(0); x++)
            {
                Point3D left = halfSide * (x - midX) + forward;
                for (int y = default; y < objects.GetLength(1); y++)
                {
                    Point3D point = left + (halfUp * (y - midY));
                }
            }
            */

            Point3D p1 = halfSide * -midX + forward + (halfUp * midY);
            Point3D p2 = halfSide * midX + forward + (halfUp * midY);
            Point3D p3 = halfSide * midX + forward + (halfUp * -midY);
            Point3D p4 = halfSide * -midX + forward + (halfUp * -midY);

            imageData.DrawLine(projection, p1, p2);
            imageData.DrawLine(projection, p2, p3);
            imageData.DrawLine(projection, p3, p4);
            imageData.DrawLine(projection, p4, p1);
        }
    }
}