namespace Ascii3dEngine
{
    public class BrightnessFitter : CharacterFitter
    {
        public BrightnessFitter(bool[,] imageData, CharMap map)
            : base(imageData, map) { }

        protected override char BestChar(int row, int startingY, int endingY, int column, int startingX, int endingX)
        {
            int count = default;
            double sum = default;
            // Start looping over the pixels (columns) that would be coverted by this charecter
            for (int x = startingX; x < endingX; x++)
            {
                // loop over the pixels (hight) that would be coverted
                for (int y = startingY; y < endingY; y++)
                {
                    if (ImageData[x, y])
                    {
                        count++;
                    }
                }
            }

            if (sum > 0)
            {
                count = (int)sum;
            }

            return Map.PickFromCount(count);
        }
    }
}