namespace Ascii3dEngine
{
    // We could look at some like this https://github.com/boxofyellow/ImageProcessing/commit/636548b08865ca507c062a1b704750bbb6d5f42f
    // to avoid all the random array access in a shared array.  But it really does not seem to make a big difference. 
    public class LineFitter : CharacterFitter
    {
        public LineFitter(bool[,] imageData, CharMap map)
            : base(imageData, map)
        {
            m_scorer = new RingBasedPixelScorer();
            m_width = imageData.GetLength(default);
            m_height = imageData.GetLength(1);
            m_scores = new (int DistanceToBlack, int DistanceToWhite)[m_width, m_height];
        }

        protected override void PreProcesses()
        {
            // we need to compute one to start
            (int DistanceToBlack, int DistanceToWhite) last = m_scorer.ComputeScoreForPixel(x: default, y: default, ImageData, neighbor: default, m_width, m_height);
            m_scores[default, default] = last;

            //need to compute down one side
            for (int y = default; y < m_height; y++)
            {
                (int DistanceToBlack, int DistanceToWhite) next = m_scorer.ComputeScoreForPixel(x: default, y, ImageData, last, m_width, m_height);
                m_scores[default, y] = next;
                last = next;
            }

            // The rest will get filled in when we process the 0 column for each row
        }

        protected override char BestChar(int row, int startingY, int endingY, int column, int startingX, int endingX)
        {
            if (column == default)
            {
                // we need to finish populating before we can continue
                for (int y = startingY; y < endingY; y++)
                {
                    // We already computed this in PreProcesses
                    (int DistanceToBlack, int DistanceToWhite) last = m_scores[default, y];

                    // Compute every other (starting after 0, and average the results)
                    for (int x = 2; x < m_width; x += 2)
                    {
                        (int DistanceToBlack, int DistanceToWhite) next = m_scorer.ComputeScoreForPixel(x, y, ImageData, last, m_width, m_height);
                        m_scores[x, y] = next;
                        m_scores[x -1, y] = ((next.DistanceToBlack + last.DistanceToBlack)/2, (next.DistanceToWhite + last.DistanceToWhite)/2);
                        last = next;
                    }
                }
            }

            int bestChar = -1;
            int bestCharVal = int.MaxValue;
            for (int i = CharMap.MinChar; i < CharMap.MaxChar; i++)
            {
                if (Map.HasData(i))
                {
                    // the running sum for this character
                    int localVal = default;

                    // Max Width and Height of this character
                    int localMaxX = Map.LocalX(i);
                    int localMaxY = Map.LocalY(i);

                    bool shouldSkip = false;

                    // Start looping over the pixels (columns) that would be coverted by this character
                    for (int x = startingX; x < endingX; x++)
                    {
                        // offset us back into our charMap
                        int offsetX = x - startingX;

                        // loop over the pixels (hight) that would be coverted
                        for (int y = startingY; y < endingY; y++)
                        {
                            // offset us back into our charMap
                            int offsetY = y - startingY;

                            // find this state we will be looking for
                            bool isBlack = offsetX < localMaxX && offsetY < localMaxY && Map.IsSet(i, offsetX, offsetY);

                            (int distanceToBlack, int distanceToWhite) = m_scores[x, y];

                            // We want to bump our count by how far away this item is
                            localVal += isBlack ? distanceToBlack : distanceToWhite; 

                            // If local is past and we are not use inverse then we can stop looking
                            if (localVal > bestCharVal)
                            {
                                shouldSkip = true;
                                break;
                            }
                        }

                        if (shouldSkip)
                        {
                            break;
                        }
                    }

                    if (localVal < bestCharVal)
                    {
                        bestCharVal = localVal;
                        bestChar = i;
                    }
                }
            }

            return (char)bestChar;
        }

        private readonly RingBasedPixelScorer m_scorer;

        private readonly (int DistanceToBlack, int DistanceToWhite)[,] m_scores;

        private readonly int m_width;
        private readonly int m_height;
    }
}