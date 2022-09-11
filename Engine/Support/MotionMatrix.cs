using System;

namespace Ascii3dEngine.Engine
{
    // This class holds information necessary to transformation point in 3D space
    // The types of transformations that it supports are as follows
    // - Scaling in 3 dimentions independently
    // - Rotating about each of the 3 axes independently
    // - Translation to a different center in 3 dimentions independently
    //
    // This also has the ability to undo this transition, this allows us to take the intersection points that and map them back
    // to where they should have landed without any of the transformation
    //
    // Some things to note
    //   Order of operations is important, we could have use 4d transformation matrixes 
    //   Doing so that would allow us to simply apply each change to the matrix as given and we could undo them by computing the inverse matrix
    //   Commuting the inverse matrix of an arbitary 4x4 matrix should be doable (assuming it determinate is not 0, which this should not)
    //   But that can be computationally expensive.
    //   Plus b/c of order of operating matters it get tricky
    //     Basically translate laterally along the X axis, then rotate about the Y axis is very different if those operations are reversed
    //   So instead we ...
    //   - Track Scaling, which is applied first
    //   - Keep one 3x3 matrix for all hte rotations and these matrix are easy to compute the inverse of, its just the transpose
    //   - Track translation
    //   Regardless of the order the mutation methods are call we will apply them in this order, and undo them in the reverse

    public class MotionMatrix
    {
        public Point3D Apply(Point3D point)
        {
            if (m_isIdentity)
            {
                return point;
            }
            
            point = new(
                point.X * Scale.X,
                point.Y * Scale.Y,
                point.Z * Scale.Z
            );

            return new(
                point.X * m_rotationMatrix[0, 0] + point.Y * m_rotationMatrix[0, 1] + point.Z * m_rotationMatrix[0, 2] + Translation.X,
                point.X * m_rotationMatrix[1, 0] + point.Y * m_rotationMatrix[1, 1] + point.Z * m_rotationMatrix[1, 2] + Translation.Y,
                point.X * m_rotationMatrix[2, 0] + point.Y * m_rotationMatrix[2, 1] + point.Z * m_rotationMatrix[2, 2] + Translation.Z
            );
        }

        public Point3D Unapply(Point3D point)
        {
            if (m_isIdentity)
            {
                return point;
            }

            point -= Translation;
            point = new(
                point.X * m_rotationMatrix[0, 0] + point.Y * m_rotationMatrix[1, 0] + point.Z * m_rotationMatrix[2, 0],
                point.X * m_rotationMatrix[0, 1] + point.Y * m_rotationMatrix[1, 1] + point.Z * m_rotationMatrix[2, 1],
                point.X * m_rotationMatrix[0, 2] + point.Y * m_rotationMatrix[1, 2] + point.Z * m_rotationMatrix[2, 2]
            );

            return new (
                point.X / Scale.X,
                point.Y / Scale.Y,
                point.Z / Scale.Z
            );
        }

        public bool IsIdentity => m_isIdentity;

        public MotionMatrix MoveTo(Point3D point)
        {
            m_isIdentity = false;
            Translation = point;
            return this;
        }

        public MotionMatrix MoveBy(Point3D point)
        {
            m_isIdentity = false;
            Translation += point;
            return this;
        }

        public MotionMatrix SetScale(Point3D scale)
        {
            m_isIdentity = false;
            Scale = scale;
            return this;
        }

        // Given two from vectors (from and up, which should be perpendicular) update the rotation matrix so
        // those vectors should get translated to the new normal and up
        public MotionMatrix Alight(Point3D fromNormal, Point3D fromUp, Point3D toNormal, Point3D toUp)
        {
            // TODO We should assert that:
            //  norma1, up1 are perpendicular
            //  norma2, up2 are perpendicular
            //  all of these are  normalized

            m_isIdentity = false;

            // TODO: this is assuming normal1 is the Z unit Vector

            // soh-cah-toa
            //rotate around the Y, to shift it into X/Z o = x, a = z,
            var angle = Math.Atan2(toNormal.X, toNormal.Z);
            // TODO, why is the "-"
            RotateByY(-angle);

            var intimidate = Apply(toNormal);

            // rotate around the X, to shift into the Y/Z o =y, a = z
            angle = Math.Atan2(intimidate.Y, intimidate.Z);
            RotateByX(angle);

            // TODO: we should continue to align up1 and up2

            // TODO: this transpose is really only valid b/c If we started with Identity matrix
            m_rotationMatrix = new double[,]
            { 
                { m_rotationMatrix[0, 0], m_rotationMatrix[1, 0], m_rotationMatrix[2, 0] },
                { m_rotationMatrix[0, 1], m_rotationMatrix[1, 1], m_rotationMatrix[2, 1] },
                { m_rotationMatrix[0, 2], m_rotationMatrix[1, 2], m_rotationMatrix[2, 2] },
            };

            return this;
        }

        public MotionMatrix RotateByX(double angle)
        {
            m_isIdentity = false;

            /* https://en.wikipedia.org/wiki/Rotation_matrix
            1, 0,  0
            0, c, -s
            0, s,  c
            */
            var c = Math.Cos(angle);
            var s = Math.Sin(angle);
            
            m_rotationMatrix = new double[,]
            {
                {
                    m_rotationMatrix[0, 0],                                  // 1[0,0] + 0[1,0] + 0[2, 0]
                    m_rotationMatrix[0, 1],                                  // 1[0,1] + 0[1,1] + 0[2, 1]
                    m_rotationMatrix[0, 2],                                  // 1[0,2] + 0[1,2] + 0[2, 2]
                },
                {
                    c * m_rotationMatrix[1, 0] - s * m_rotationMatrix[2, 0], // 0[0,0] + c[1,0] - s[2, 0]
                    c * m_rotationMatrix[1, 1] - s * m_rotationMatrix[2, 1], // 0[0,1] + c[1,1] - s[2, 1]
                    c * m_rotationMatrix[1, 2] - s * m_rotationMatrix[2, 2], // 0[0,2] + c[1,2] - s[2, 2]
                },
                {
                    s * m_rotationMatrix[1, 0] + c * m_rotationMatrix[2, 0], // 0[0,0] + s[1,0] + c[2, 0]
                    s * m_rotationMatrix[1, 1] + c * m_rotationMatrix[2, 1], // 0[0,1] + s[1,1] + c[2, 1]
                    s * m_rotationMatrix[1, 2] + c * m_rotationMatrix[2, 2], // 0[0,2] + s[1,2] + c[2, 2]
                },
            };

            return this;
        }

        public MotionMatrix RotateByY(double angle)
        {
            m_isIdentity = false;

            /* https://en.wikipedia.org/wiki/Rotation_matrix
             c, 0, s
             0, 1, 0
            -s, 0, c
            */
            var c = Math.Cos(angle);
            var s = Math.Sin(angle);
            
            m_rotationMatrix = new double[,]
            {
                {
                    c * m_rotationMatrix[0, 0] + s * m_rotationMatrix[2, 0],  // c[0,0] + 0[1,0] + s[2, 0]
                    c * m_rotationMatrix[0, 1] + s * m_rotationMatrix[2, 1],  // c[0,1] + 0[1,1] + s[2, 1]
                    c * m_rotationMatrix[0, 2] + s * m_rotationMatrix[2, 2],  // c[0,2] + 0[1,2] + s[2, 2]
                },
                {
                    m_rotationMatrix[1, 0],                                   // 0[0,0] + 1[1,0] + 0[2, 0]
                    m_rotationMatrix[1, 1],                                   // 0[0,1] + 1[1,1] + 0[2, 1]
                    m_rotationMatrix[1, 2],                                   // 0[0,2] + 1[1,2] + 0[2, 2]
                },
                {
                    // TODO: would c - s be faster than -s + c... I would hope the compiler can do optimizations
                    -s * m_rotationMatrix[0, 0] + c * m_rotationMatrix[2, 0], // -s[0,0] + 0[1,0] + c[2, 0]
                    -s * m_rotationMatrix[0, 1] + c * m_rotationMatrix[2, 1], // -s[0,1] + 0[1,1] + c[2, 1]
                    -s * m_rotationMatrix[0, 2] + c * m_rotationMatrix[2, 2], // -s[0,2] + 0[1,2] + c[2, 2]
                },
            };

            return this;
        }

        public MotionMatrix RotateByZ(double angle)
        {
            m_isIdentity = false;

            /* https://en.wikipedia.org/wiki/Rotation_matrix
            c, -s, 0
            s,  c, 0
            0,  0, 1
            */
            var c = Math.Cos(angle);
            var s = Math.Sin(angle);
            
            m_rotationMatrix = new double[,]
            {
                {
                    c * m_rotationMatrix[0, 0] - s * m_rotationMatrix[1, 0], // c[0,0] - s[1,0] + 0[2, 0]
                    c * m_rotationMatrix[0, 1] - s * m_rotationMatrix[1, 1], // c[0,1] - s[1,1] + 0[2, 1]
                    c * m_rotationMatrix[0, 2] - s * m_rotationMatrix[1, 2], // c[0,2] - s[1,2] + 0[2, 2]
                },
                {
                    s * m_rotationMatrix[0, 0] + c * m_rotationMatrix[1, 0], // s[0,0] + c[1,0] + 0[2, 0]
                    s * m_rotationMatrix[0, 1] + c * m_rotationMatrix[1, 1], // s[0,1] + c[1,1] + 0[2, 1]
                    s * m_rotationMatrix[0, 2] + c * m_rotationMatrix[1, 2], // s[0,2] + c[1,2] + 0[2, 2]
                },
                {
                    m_rotationMatrix[2, 0],                                  // 0[0,0] + 0[1,0] + 1[2, 0]
                    m_rotationMatrix[2, 1],                                  // 0[0,1] + 0[1,1] + 1[2, 1]
                    m_rotationMatrix[2, 2],                                  // 0[0,2] + 0[1,2] + 1[2, 2]
                },
            };

            return this;
        }

        public Point3D Translation { get; private set; }

        public Point3D Scale { get; private set; } = Point3D.Identity;

        // TODO: should we make changes to help limit the number of arrays that allocate, should we change this to only allocate 
        // this one on the heep and all the "temp" ons the stack?
        private double[,] m_rotationMatrix = new double[,]
        {
            {Point3D.XUnit.X, Point3D.XUnit.Y, Point3D.XUnit.Z},
            {Point3D.YUnit.X, Point3D.YUnit.Y, Point3D.YUnit.Z},
            {Point3D.ZUnit.X, Point3D.ZUnit.Y, Point3D.ZUnit.Z},
        };

        private bool m_isIdentity = true;
    }
}