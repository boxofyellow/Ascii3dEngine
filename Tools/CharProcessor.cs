using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public static class CharProcessor
{
    public static (int[] Counts, int Width, int Height, Rgb24[] NamedColors) ComputeCharCounts(string filePath, int itemsPerRow)
    {
        Rgb24[,] imageData;
        using (var image = Image.Load<Rgb24>(filePath))
        {
            imageData = image.GetPixelData();
        }
        AssertIsColor(imageData, pixelRow: 0, pixelCol: 0, Color.White);

        int topPixelRow = -1;
        int leftPixelCol = -1;

        for (int tryValue = 0; tryValue < Math.Min(imageData.GetLength(0), imageData.GetLength(1)) ;tryValue++)
        {
            // check for left
            if (leftPixelCol < 0)
            {
                var check = Check(imageData, tryValue, start: 0, across: true, Color.Black, max:tryValue);
                if (check != -1)
                {
                    leftPixelCol = check;
                    topPixelRow = Check(imageData, leftPixelCol, start: 0, across: false, Color.Black);
                    break;
                }
            }

            // check for top
            if (topPixelRow < 0)
            {
                var check = Check(imageData, tryValue, start: 0, across: false, Color.Black, max:tryValue);
                if (check != -1)
                {
                    topPixelRow = check;
                    topPixelRow = Check(imageData, topPixelRow, start: 0, across: true, Color.Black);
                    break;
                }
            }
        }

        if (leftPixelCol == -1 || topPixelRow == -1)
        {
            throw new ApplicationException($"Failed to find the corner of the black square inside the the white box {nameof(leftPixelCol)}: {leftPixelCol}, {nameof(topPixelRow)}: {topPixelRow}");
        }

        int charWidth;
        int charHeight;
        {
            // Find character width
            var check = Check(imageData, at: topPixelRow, start: leftPixelCol, across: true, Color.White);
            if (check == -1)
            {
                throw new ApplicationException($"Failed to find the character width {nameof(leftPixelCol)}: {leftPixelCol}, {nameof(topPixelRow)}: {topPixelRow}");
            }
            charWidth = check - leftPixelCol;

            // Find character height
            check = Check(imageData, at : leftPixelCol, start: topPixelRow, across: false, Color.White);
            if (check == -1)
            {
                throw new ApplicationException($"Failed to find the character height {nameof(leftPixelCol)}: {leftPixelCol}, {nameof(topPixelRow)}: {topPixelRow}");
            }
            charHeight = check - topPixelRow;
        }

        Console.WriteLine($"{nameof(leftPixelCol)}: {leftPixelCol}, {nameof(topPixelRow)}: {topPixelRow}, {nameof(charWidth)}:{charWidth}, {nameof(charHeight)}:{charHeight}");

        var charRow = 2; // The top rim of the white square does not count
        var charCol = 0; // The left rim of the white square does not count

        double charArea = (double)(charWidth * charHeight);

        var colors = new Rgb24[(int)ConsoleColor.White + 1];
        colors[(int)ConsoleColor.Black] = Color.Black.ToPixel<Rgb24>();
        for (int colorIndex = (int)ConsoleColor.Black + 1; colorIndex < (int)ConsoleColor.White; colorIndex++, charCol++)
        {
            (var pixelRow, var pixelCol) = ToPixelFromChar(topPixelRow, leftPixelCol, charHeight, charWidth, charRow, charCol);

            int sumRed = 0;
            int sumGreen = 0;
            int sumBlue = 0;

            int maxRed = int.MinValue;
            int maxGreen = int.MinValue;
            int maxBlue = int.MinValue;

            int minRed = int.MaxValue;
            int minGreen = int.MaxValue;
            int minBlue = int.MaxValue;

            for (var y = 0; y < charHeight; y++)
            for (var x = 0; x < charWidth; x++)
            {
                var color = imageData[pixelRow + y, pixelCol + x];

                sumRed += (int)color.R;
                sumGreen += (int)color.G;
                sumBlue += (int)color.B;

                maxRed = Math.Max(maxRed, (int)color.R);
                maxGreen = Math.Max(maxGreen, (int)color.G);
                maxBlue = Math.Max(maxBlue, (int)color.B);

                minRed = Math.Min(minRed, (int)color.R);
                minGreen = Math.Min(minGreen, (int)color.G);
                minBlue = Math.Min(minBlue, (int)color.B);
            }

            if ((maxRed - minRed > 3)
                || (maxGreen - minGreen > 3)
                || (maxBlue - minBlue > 3))
            {
                throw new ApplicationException($@"To much change at {nameof(charRow)}: {charRow}, {nameof(charCol)}: {charCol}
{nameof(maxRed)}: {maxRed}, {nameof(minRed)}: {minRed}, {nameof(maxGreen)}: {maxGreen}, {nameof(minGreen)}: {minGreen}, {nameof(maxBlue)}: {maxBlue}, {nameof(minBlue)}: {minBlue}");
            }

            colors[colorIndex] = new (
                (byte)Math.Round((double)sumRed / charArea),
                (byte)Math.Round((double)sumGreen / charArea),
                (byte)Math.Round((double)sumBlue / charArea));

            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write("(");
            Console.BackgroundColor = (ConsoleColor)colorIndex;
            Console.Write(" ");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine($") {colors[colorIndex]} {(ConsoleColor)colorIndex}");
        }
        colors[(int)ConsoleColor.White] = Color.White.ToPixel<Rgb24>();

        var blue = colors[(int)ConsoleColor.Blue];      // This is far off from 0,0,255
        var green = colors[(int)ConsoleColor.Green];    // same for             0,255,0

        var charCounts = new int[c_maxChar + 1];
        int charIndex;
        for (charIndex = 0; charIndex < CharMap.MinChar; charIndex++)
        {
            charCounts[charIndex] = -1;
        }

        int displayColumn = 0;

        while (charIndex < c_maxChar) // loop on rows
        {
            charRow++;

            charCol = 0;
            (var pixelRow, var pixelCol) = ToPixelFromChar(topPixelRow, leftPixelCol, charHeight, charWidth, charRow, charCol);
            AssertIsColor(imageData, pixelRow, pixelCol, blue);

            charRow++;

            while (true)
            {           
                (pixelRow, pixelCol) = ToPixelFromChar(topPixelRow, leftPixelCol, charHeight, charWidth, charRow, charCol);

                if (AreClose(imageData[pixelRow, pixelCol], green))
                {
                    // reached the end of the line
                    break;
                }

                AssertIsColor(imageData, pixelRow, pixelCol, Color.Black);

                charCol += 6; // We are here V
                              //            B ###:[ ]B
                (pixelRow, pixelCol) = ToPixelFromChar(topPixelRow, leftPixelCol, charHeight, charWidth, charRow, charCol);

                var sum = 0;
                for (var y = 0; y < charHeight; y++)
                for (var x = 0; x < charWidth; x++)
                {
                    var color = imageData[pixelRow + y, pixelCol + x];
                    sum += (int)color.R + (int)color.G + (int)color.B;   
                }

                var count = (int)Math.Round((double)sum / c_countDevisor);
                charCounts[charIndex] = count;

                Console.Write($" {charIndex,3}:({(char)charIndex})={count,3}");

                if (displayColumn++ >= itemsPerRow)
                {
                    displayColumn = 0;
                    Console.WriteLine();
                }

                charCol += 2; // one to skip the ], and then 1 to move to the blue

                (pixelRow, pixelCol) = ToPixelFromChar(topPixelRow, leftPixelCol, charHeight, charWidth, charRow, charCol);
                AssertIsColor(imageData, pixelRow, pixelCol, blue);

                charCol++; // Move to the next item
                charIndex++;
            }
        }

        Console.WriteLine();

        return (charCounts, charWidth, charHeight, colors);
    }

    private static void AssertIsColor(Rgb24[,] imageData, int pixelRow, int pixelCol, Color color)
    {
        var found = imageData[pixelRow, pixelCol];
        var expected = color.ToPixel<Rgb24>();
        if (!AreClose(found, expected))
        {
            throw new ApplicationException($"At {nameof(pixelRow)}: {pixelRow}, {nameof(pixelCol)}: {pixelCol}, {nameof(found)}: {found}, {nameof(expected)}: {expected}");
        }
    }

    private static int Check(Rgb24[,] imageData, int at, int start, bool across, Color color, int max = -1)
    {
        if (max == -1)
        {
            max = across
              ? imageData.GetLength(1)
              : imageData.GetLength(0);

            // since we <= in the for loop
            max--;
        }
        var target = color.ToPixel<Rgb24>();
        for (int result = start; result <= max; result++)
        {
            var found = across
              ? imageData[at, result]  // scan horizontally 
              : imageData[result, at]; // scan vertically

            if (AreClose(found, target))
            {
                return result;
            }
        }

        return -1; // we did not find it.
    }

    private static (int PixelRow, int PixelColumn) ToPixelFromChar(int topPixelRow, int leftPixelCol, int charHeight, int charWidth, int charRow, int charCol)
    {
        return new (
            topPixelRow + (charRow * charHeight),
            leftPixelCol + (charCol * charWidth)
        );
    }

    private static bool AreClose(Rgb24 c1, Rgb24 c2) => Math.Abs(c1.R - c2.R) < 3
                                                     && Math.Abs(c1.G - c2.G) < 3
                                                     && Math.Abs(c1.B - c2.B) < 3;

    private const int c_maxChar = (int)byte.MaxValue;

    private const double c_countDevisor = 3.0                     // 3 b/c there are 3 color components 
                                      * (double)byte.MaxValue;  // 255 is the max value they can have    
}