using System.Runtime.CompilerServices;

namespace Ascii3dEngine
{
    public readonly struct ColorProperties
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public ColorProperties(Point3D ambient, Point3D diffuse, Point3D specular, double shininess)
        {
            Ambient = ambient;
            Diffuse = diffuse;
            Specular = specular;
            Shininess = shininess;
        }

        public readonly Point3D Ambient;
        public readonly Point3D Diffuse;
        public readonly Point3D Specular;
        public readonly double Shininess;

        // From 423, but here are some more. http://www.barradeau.com/nicoptere/dump/materials.html
        public static ColorProperties BlackPlastic = new ColorProperties(
            new Point3D(0.0, 0.0, 0.0),
            new Point3D(0.01, 0.01, 0.01),
            new Point3D(0.5, 0.5, 0.5),
            32);

        public static ColorProperties Brass = new ColorProperties(
            new Point3D(0.329412, 0.223529, 0.027451),
            new Point3D(0.780392, 0.568627, 0.113725),
            new Point3D(0.992157, 0.941176, 0.807843),
            27.8974);

        public static ColorProperties Bronze = new ColorProperties(
            new Point3D(0.2125, 0.1275, 0.054),
            new Point3D(0.714, 0.4284, 0.18144),
            new Point3D(0.393548, 0.271906, 0.166721),
            25.6);

        public static ColorProperties Chrome = new ColorProperties(
            new Point3D(0.25, 0.25, 0.25),
            new Point3D(0.4, 0.4, 0.4),
            new Point3D(0.774597, 0.774597, 0.774597),
            76.8);

        public static ColorProperties Copper = new ColorProperties(
            new Point3D(0.19125, 0.0735, 0.0225),
            new Point3D(0.7038, 0.27048, 0.828),
            new Point3D(0.256777, 0.137622, 0.086014),
            12.8);

        public static ColorProperties Gold = new ColorProperties(
            new Point3D(0.24725, 0.1995, 0.0745),
            new Point3D(0.75164, 0.60648, 0.22648),
            new Point3D(0.628281, 0.555802, 0.366065),
            51.2);

        public static ColorProperties Pewter = new ColorProperties(
            new Point3D(0.10588, 0.058824, 0.113725),
            new Point3D(0.427451, 0.470588, 0.541176),
            new Point3D(0.3333, 0.3333, 0.521569),
            9.84615);

        public static ColorProperties Silver = new ColorProperties(
            new Point3D(0.19225, 0.19225, 0.19225),
            new Point3D(0.50754, 0.50754, 0.50754),
            new Point3D(0.508273, 0.508273, 0.508273),
            51.2);

        public static ColorProperties PolishedSilver = new ColorProperties(
            new Point3D(0.23125, 0.23125, 0.23125),
            new Point3D(0.2775, 0.2775, 0.2775),
            new Point3D(0.773911, 0.773911, 0.773911),
            89.6);

        // http://www.sci.tamucc.edu/~sking/Courses/COSC5327/Assignments/Materials.html (normalized)
        public static ColorProperties WhitePlastic = new ColorProperties(
            new Point3D(0.0, 0.0, 0.0),
            new Point3D(0.55, 0.55, 0.55),
            new Point3D(0.7, 0.7, 0.7),
            32);

        public static ColorProperties CyanPlastic = new ColorProperties(
            new Point3D(0.0, 0.0, 0.0),
            new Point3D(0.55, 0.0, 0.55),
            new Point3D(0.7, 0.7, 0.7),
            32);

        public static ColorProperties GreenPlastic = new ColorProperties(
            new Point3D(0.0, 0.0, 0.0),
            new Point3D(0.0, 0.55, 0.0),
            new Point3D(0.7, 0.7, 0.7),
            32);

        public static ColorProperties RedPlastic = new ColorProperties(
            new Point3D(0.0, 0.0, 0.0),
            new Point3D(0.5, 0.0, 0.0),
            new Point3D(0.7, 0.7, 0.7),
            32);

        public static ColorProperties YellowPlastic = new ColorProperties(
            new Point3D(0.0, 0.0, 0.0),
            new Point3D(0.55, 0.55, 0.0),
            new Point3D(0.7, 0.7, 0.7),
            32);

        // using the above to assume the rest

        public static ColorProperties BluePlastic = new ColorProperties(
            new Point3D(0.0, 0.0, 0.0),
            new Point3D(0.0, 0.0, 0.55),
            new Point3D(0.7, 0.7, 0.7),
            32);

        public static ColorProperties PurplePlastic = new ColorProperties(
            new Point3D(0.0, 0.0, 0.0),
            new Point3D(0.55, 0.0, 0.55),
            new Point3D(0.7, 0.7, 0.7),
            32);
    }
}