namespace Ascii3dEngine
{
    public static class CubeDefinition
    {
        public readonly static Point3D[] Points = new [] {
            new Point3D(-1, 1, 1),   // font upper left
            new Point3D(1, 1, 1),    // font upper right
            new Point3D(-1, -1, 1),  // font lower left
            new Point3D(1, -1, 1),   // font lower right

            new Point3D(-1, 1, -1),  // back upper left
            new Point3D(1, 1, -1),   // back upper right
            new Point3D(-1, -1, -1), // back lower left
            new Point3D(1, -1, -1),  // back lower right
        };

        public const int FrontUpperLeft =  0;
        public const int FrontUpperRight =  1;
        public const int FrontLowerLeft =  2;
        public const int FrontLowerRight =  3;
        public const int BackUpperLeft =  4;
        public const int BackUpperRight =  5;
        public const int BackLowerLeft =  6;
        public const int BackLowerRight =  7;

        public readonly static int[][] Faces = new [] {
            new [] {FrontUpperRight, FrontUpperLeft, FrontLowerLeft, FrontLowerRight}, // Front
            new [] {BackUpperLeft, BackUpperRight, BackLowerRight, BackLowerLeft},     // Back
            new [] {BackUpperRight, FrontUpperRight, FrontLowerRight, BackLowerRight}, // Right
            new [] {FrontUpperLeft, BackUpperLeft, BackLowerLeft, FrontLowerLeft},     // Left
            new [] {FrontUpperLeft, FrontUpperRight, BackUpperRight, BackUpperLeft},   // top
            new [] {FrontLowerRight, FrontLowerLeft, BackLowerLeft, BackLowerRight},   // bottom
        };

        public readonly static Point3D[] Normals = new [] {
            new Point3D(0, 0, 1),  // Front
            new Point3D(0, 0, -1), // Back
            new Point3D(1, 0, 0),  // Right
            new Point3D(-1, 0, 0), // Left
            new Point3D(0, 1, 0),  // top
            new Point3D(0, -1, 0), // bottom
        };
    }
}