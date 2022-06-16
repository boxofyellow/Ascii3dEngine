using System;
using System.Threading.Tasks;

namespace Ascii3dEngine
{
    public abstract class CharacterFitter
    {
        public static CharacterFitter Create(Settings settings, bool[,] imageData, CharMap map) => settings.UseLineFitter 
            ? new LineFitter(imageData, map)
            : new BrightnessFitter(imageData, map);

        protected CharacterFitter(bool[,] imageData, CharMap map)
        {
            ImageData = imageData;
            Map = map;
        }

        public string[] ComputeChars(Settings settings)
        {
            int columns = Utilities.Ratio(ImageData.GetLength(0), Map.MaxX);
            int rows = Utilities.Ratio(ImageData.GetLength(1), Map.MaxY);

            if (true)
            {
                //throw new Exception($"{columns}, {rows}");
            }


            // This is a lot of processing to do you may think that we could cache some of the results,
            // But it turns out that does not work out so well, basically many images have "fuzz" so you are unlikely to find exact matches
            // And since we look beyond the space that would be covered by the character you need consider a very large area
            // So if you can't cache it, build an algorithm to limit the search (and extra hardware does not hurt)
            PreProcesses();
            //
            // Loop over each row of characters
            var lines = new string[rows];

            Parallel.For(
                fromInclusive: default,
                toExclusive: rows,
                parallelOptions: new() { MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism },
                (int row) =>
            {
                var line = new char[columns];

                // We will loop y from the first pixel (at the top) that starts this row all the way down the hight of a character (or the end of the data, which ever comes first)
                int startingY = row * Map.MaxY;
                int endingY = Math.Min(startingY + Map.MaxY, ImageData.GetLength(1));

                // Look over each column of characters
                for (int column = default; column < columns; column++)
                {
                    // we will loop x from the first pixel (on the left) that starts this column all the way right (the width of one character) or until we run out
                    int startingX = column * Map.MaxX;
                    int endingX = Math.Min(startingX + Map.MaxX, ImageData.GetLength(0));

                    // Now try to pick the best matching character
                    line[column] = BestChar(row, startingY, endingY, column, startingX, endingX);
                }
                lines[row] = new string(line);
            });

            return lines;
        }

        protected readonly CharMap Map;

        protected readonly bool[,] ImageData;

        protected abstract char BestChar(int row, int startingY, int endingY, int column, int startingX, int endingX);

        protected virtual void PreProcesses() { }
    }
}