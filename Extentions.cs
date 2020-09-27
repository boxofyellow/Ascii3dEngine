using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Ascii3dEngine
{
    public static class Extentions
    {
        public static bool IsBlack<TSelf>(this IPixel<TSelf> pixel) where TSelf : struct, IPixel<TSelf>
        {
            IPixel<TSelf> black = Color.Black.ToPixel<TSelf>(); 
            if (pixel.Equals(black))
            {
                return true;
            }

            IPixel<TSelf> white = Color.White.ToPixel<TSelf>();
            if (pixel.Equals(white))
            {
                return false;
            }

            throw new Exception($"Found pixel ({pixel}) that is not white ({white}) or black ({black})");
        }

        public static void DrawLine(this bool[,] target, Projection projection, Point3D start, Point3D end)
        {
            (bool inView, Point2D p1, Point2D p2) = projection.Trans_Line(start, end);
            if (inView)
            {
                target.DrawLine(p1, p2);
            }
        }

        /// <summary>
        /// Draw a line form start to end on target
        /// find out covers more ground, change in X or change in Y
        /// we will eventually do a for loop over that longer range and mark all the "pixesl"
        /// But we need to figure out how often (and in which direction) we should change axes that we are not loop over
        /// And we need to give our line a little thickness so fill 4 pixes on other side of the line
        /// </summary>
        public static void DrawLine(this bool[,] target, Point2D start, Point2D end)
        {
            double dV = end.V - start.V;
            double dH = end.H - start.H;

            double absDV = Math.Abs(dV);
            double absDH = Math.Abs(dH);

            if (absDV > absDH)
            {
                float changeHEvery = absDH > 0 ? (float)(absDV / absDH) : 0;
                int changeHBy = absDH > 0 ? (dH > 0 ? 1 : -1) : 0;

                int startV;
                int endV;
                int h;
                int max = target.GetLength(1) - 1;
                if (dV > 0)
                {
                    startV = Math.Min((int)start.V, max);
                    endV = Math.Min((int)end.V, max);
                    h = (int)start.H;
                }
                else
                {
                    startV = Math.Min((int)end.V, max);
                    endV = Math.Min((int)start.V, max);
                    h = (int)end.H;
                    changeHBy *= -1;
                }

                int count = 0;
                int lastChange = 0;
                max = target.GetLength(0);
                for (int v = startV; v < endV; v++)
                {
                    count++;
                    int check = (int)(count / changeHEvery);
                    if (check > lastChange)
                    {
                        lastChange = check;
                        h += changeHBy;
                    }

                    for (int i = -4; i < 5; i++)
                    {
                        int newH = h + i;
                        if (newH >= 0 && newH < max)
                        {
                            target[newH, v] = true;
                        }
                    }
                }
            }
            else
            {
                float changeVEvery = absDV > 0 ? (float)(absDH / absDV) : 0;
                int changeVBy = absDV > 0 ? (dV > 0 ? 1 : -1) : 0;

                int startH;
                int endH;
                int v;
                int max = target.GetLength(0) - 1;
                if (dH > 0)
                {
                    startH = Math.Min((int)start.H, max);
                    endH = Math.Min((int)end.H, max);
                    v = (int)start.V;
                }
                else
                {
                    startH = Math.Min((int)end.H, max);
                    endH = Math.Min((int)start.H, max);
                    v = (int)end.V;
                    changeVBy *= -1;
                }

                int count = 0;
                int lastChange = 0;
                max = target.GetLength(1);
                for (int h = startH; h < endH; h++)
                {
                    count++;
                    int check = (int)(count / changeVEvery);
                    if (check > lastChange)
                    {
                        lastChange = check;
                        v += changeVBy;
                    }

                    for (int i = -4; i < 5; i++)
                    {
                        int newV = v + i;
                        if (newV >= 0 && newV < max)
                        {
                            target[h, newV] = true;
                        }
                    }
                }
            }
        }
    }
}
