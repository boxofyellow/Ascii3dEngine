using System;
using System.Runtime.CompilerServices;

namespace Ascii3dEngine.Engine
{
    public readonly struct Point3D
    {
        public readonly double X, Y, Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point3D(double x, double y, double z)
        {
            X = x; Y = y; Z = z;
        }

        public static Point3D Parse(string? value, Point3D? defaultValue = null)
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue ?? Zero;
            }

            string temp = value.TrimStart('{').TrimEnd('}');
            string[] pieces = temp.Split(",");
            try
            {
                return new(
                    double.Parse(pieces[0].Trim()),
                    double.Parse(pieces[1].Trim()),
                    double.Parse(pieces[2].Trim())
                );
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Failed to parse {value} {ex}");
                throw;
            }
        }

        // since we will often be dividing by this (see Normalized) we might want to use Quake's fast InvSqrt function (https://en.wikipedia.org/wiki/Fast_inverse_square_root)
        // But that is not really going to get us much in the way of savings see https://stackoverflow.com/questions/268853/is-it-possible-to-write-quakes-fast-invsqrt-function-in-c
        // Additionally we should also just be mindful to see if we really do need the Square at all see ColorUtilities.BestMatch
        public double Length {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point3D Normalized() => this / Length;

        public bool IsZero { 
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this == Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point3D CrossProduct(Point3D vector) => new(
                (Y * vector.Z) - (Z * vector.Y),
                (Z * vector.X) - (X * vector.Z),
                (X * vector.Y) - (Y * vector.X));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double DotProduct(Point3D other)
            => (X * other.X) + (Y * other.Y) + (Z * other.Z);

        public Point3D ApplyAffineTransformation(double[,] transformation)
        {
            if (transformation == null || transformation.GetLength(0) != 4 || transformation.GetLength(1) != 4)
            {
                throw new ArgumentException($"Not correct size ({transformation?.GetLength(0)}, {transformation?.GetLength(1)})", nameof(transformation));
            }

            // Page 216
            // Using row follow by column here
            return new(
                X * transformation[0,0] + Y * transformation[0, 1] + Z * transformation[0, 2] + transformation[0, 3],
                X * transformation[1,0] + Y * transformation[1, 1] + Z * transformation[1, 2] + transformation[1, 3],
                X * transformation[2,0] + Y * transformation[2, 1] + Z * transformation[2, 2] + transformation[2, 3]
            );
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point3D operator +(Point3D a, Point3D b) => new(
                a.X + b.X,
                a.Y + b.Y,
                a.Z + b.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point3D operator -(Point3D a, Point3D b) => new(
                a.X - b.X,
                a.Y - b.Y,
                a.Z - b.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point3D operator *(Point3D a, double n) => new(
                a.X*n,
                a.Y*n,
                a.Z*n);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point3D operator /(Point3D a, double n) => new(
                a.X/n,
                a.Y/n,
                a.Z/n);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Point3D a, Point3D b) => (a.X == b.X) 
                && (a.Y == b.Y)
                && (a.Z == b.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Point3D a, Point3D b) => (a.X != b.X) 
                || (a.Y != b.Y)
                || (a.Z != b.Z);

        public override bool Equals(object? obj) 
            => obj != null && (obj is Point3D p) && this == p;

        // This is rather poor hash code, but it will get the job done
        public override int GetHashCode()
            => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();


        public readonly static Point3D Zero = new();

        public readonly static Point3D XUnit = new(1, 0, 0);

        public readonly static Point3D YUnit = new(0, 1, 0);

        public readonly static Point3D ZUnit = new(0, 0, 1);

        public readonly static Point3D Identity = new(XUnit.X, YUnit.Y, ZUnit.Z);

        public override string ToString() => $"{{{X}, {Y}, {Z}}}";
    }
}