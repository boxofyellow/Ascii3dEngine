namespace Ascii3dEngine.Engine
{
    public static class CubeDefinition
    {
        public readonly static Point3D[] Points = new Point3D[] {
            new(-1, 1, 1),   // font upper left
            new(1, 1, 1),    // font upper right
            new(-1, -1, 1),  // font lower left
            new(1, -1, 1),   // font lower right

            new(-1, 1, -1),  // back upper left
            new(1, 1, -1),   // back upper right
            new(-1, -1, -1), // back lower left
            new(1, -1, -1),  // back lower right
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

        public readonly static Point3D[] Normals = new Point3D[] {
            Point3D.ZUnit,      // Front
            Point3D.ZUnit * -1, // Back
            Point3D.XUnit,      // Right
            Point3D.XUnit * -1, // Left
            Point3D.YUnit,      // top
            Point3D.YUnit * -1, // bottom
        };
    }
}