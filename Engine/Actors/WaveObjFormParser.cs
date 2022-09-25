// Borrowing heavily from https://github.com/Noortvel/OBJ3DWavefrontLoader
// That failed to load some "simpler" version obj files that I came across 

public static class WaveObjFormParser
{
    public static (Point3D[] Points, int[][] Faces) Parse(string filePath)
    {

        List<Point3D> points = new();
        List<int[]> faces = new();

        string[] lines = File.ReadAllLines(filePath);
        for (int i = default; i < lines.Length; i++)
        {
            string line = lines[i];
            if (!string.IsNullOrEmpty(line))
            {
                string[] pieces = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                try
                {
                    switch (pieces.First())
                    {
                        case "v":       // Vertex
                            points.Add(new(
                                double.Parse(pieces[1]),
                                double.Parse(pieces[2]),
                                double.Parse(pieces[3])));
                            break;

                        case "f":       // Face
                            faces.Add(pieces
                                .Skip(1) // Exclude the 'f'
                                .Select(x => int.Parse(x.Split('/').First()) -1)   // -1 here b/c Obj Files are not 0 indexed... go figure
                                .ToArray());
                            break;

                        case "#":       // Comment
                        case "vn":      // Normal
                        case "ut":      // UV or UT or what ever that is...., I guess I could look this up, but we are ignoring 🙃
                        case "vt":
                        case "g":
                        case "s":
                        case "o":       // appears to be a name for the object
                        case "mtllib":  // I'm not sure what this is for `mtllib teapot.mtl` When exporting things with blender these get created
                                        // These small file might have details about the materials that make up the object
                        case "usemtl":  // usemtl wire_088177027
                            break;

                        default:
                            throw new Exception($"Unknown items {line}");
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"{i}|{line}");
                    throw;
                }
            }
        }

        Console.WriteLine($"From {filePath} loaded {points.Count} vertexes and {faces.Count} faces");

        return (
            points.ToArray(),
            faces.ToArray()
        );
    }
}