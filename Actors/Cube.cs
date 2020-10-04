using System;
using System.Collections.Generic;

namespace Ascii3dEngine
{
    public class Cube : PolygonActorBase
    {
        public Cube(Settings settings, CharMap map) : base(settings)
        {
            m_map = map;
        }

        protected override (Point3D[] Points, int[][] Faces) GetData(Settings settings)
        {
            Point3D[] points = new []
            {
                new Point3D(-1, 1, 1),   // font upper left
                new Point3D(1, 1, 1),    // font upper right
                new Point3D(-1, -1, 1),  // font lower left
                new Point3D(1, -1, 1),   // font lower right

                new Point3D(-1, 1, -1),  // back upper left
                new Point3D(1, 1, -1),   // back upper right
                new Point3D(-1, -1, -1), // back lower left
                new Point3D(1, -1, -1),  // back lower right
            };

            for (int i = default; i < points.Length; i++)
            {
                points[i] = points[i] * (c_size / 2.0);
            }

            // We need to keep this and m_lables in sync
            int[][] faces = new []
            {
                new [] {c_frontUpperRight, c_frontUpperLeft, c_frontLowerLeft, c_frontLowerRight}, // Front
                new [] {c_backUpperLeft, c_backUpperRight, c_backLowerRight, c_backLowerLeft},     // Back
                new [] {c_backUpperRight, c_frontUpperRight, c_frontLowerRight, c_backLowerRight}, // Right
                new [] {c_frontUpperLeft, c_backUpperLeft, c_backLowerLeft, c_frontLowerLeft},     // Left
                new [] {c_frontUpperLeft, c_frontUpperRight, c_backUpperRight, c_backUpperLeft},   // top
                new [] {c_frontLowerRight, c_frontLowerLeft, c_backLowerLeft, c_backLowerRight},   // bottom
            };

            return (points, faces);
        }

        public override void AddLabel(int face, Projection projection, Point3D[] points, List<Label> labels)
        { 
            Point3D average = points.Average();
            (bool inView, _, Point2D projectedP2) = projection.Trans_Line(new Point3D(), average);
            if (inView)
            {
                labels.Add(new Label(
                    projectedP2.H / m_map.MaxX,
                    projectedP2.V / m_map.MaxY,
                    m_lables[face]));
            }
        }

        private readonly char[] m_lables = new [] {'F', 'B', 'R', 'L', 't', 'b'};
        private const int c_frontUpperLeft =  0;
        private const int c_frontUpperRight =  1;
        private const int c_frontLowerLeft =  2;
        private const int c_frontLowerRight =  3;
        private const int c_backUpperLeft =  4;
        private const int c_backUpperRight =  5;
        private const int c_backLowerLeft =  6;
        private const int c_backLowerRight =  7;

        private readonly CharMap m_map;
        private const double c_size = 25;
    }
}