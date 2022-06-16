using System;
using System.Collections.Generic;

namespace Ascii3dEngine
{
    public class RingBasedPixelScorer
    {
        public RingBasedPixelScorer()
        {
            m_outOfRangeCost = 100;
            m_maxRingRadius = 8;

            var rings = new List<(int Dx, int Dy)>[m_maxRingRadius + 1]; 
            for (int i = default; i < rings.Length; i++)
            {
                rings[i] = new List<(int Dx, int Dy)>();
            }
            for (int dy = -m_maxRingRadius; dy <= m_maxRingRadius; dy++)
            {
                int dyy = dy * dy;
                for (int dx = -m_maxRingRadius; dx <= m_maxRingRadius; dx++)
                {
                    double d = Math.Sqrt(dyy + (dx * dx));
                    int r = (int)Math.Ceiling(d);
                    if (r <= m_maxRingRadius)
                    {
                        rings[r].Add((dx, dy));
                    }
                }
            }

            m_rings = new (int Dx, int Dy)[rings.Length][];
            for (int i = default; i < rings.Length; i++)
            {
                m_rings[i] = rings[i].ToArray();
            }
        }
        
        public (int DistanceToBlack, int DistanceToWhite) ComputeScoreForPixel(int x, int y, bool[,] sourceArray, (int DistanceToBlack, int DistanceToWhite) neighbor, int width, int height) 
        {
            // first figure out what are we looking for
            bool foundBlack = sourceArray[x, y];
            bool foundWhite = !foundBlack;

            // Now figure out where we need to be checking
            int start;
            int end;
            if (neighbor.DistanceToBlack == default && neighbor.DistanceToWhite == default)
            {
                // Every neighbor should have at least one non-zero, so if we are in this state, we must be starting off without a neighbor
                start = default;
                end = m_maxRingRadius + 1;
            }
            else
            {
                // we will want to check +/- c_neighborRange around the where the other one left off.
                int mid = foundBlack ? neighbor.DistanceToWhite : neighbor.DistanceToBlack;
                start = Math.Min(m_maxRingRadius, Math.Max(default, mid - c_neighborRange));
                end = Math.Min(m_maxRingRadius, mid + c_neighborRange) + 1;  //+1 here since our for loop uses <
            }

            int distanceToBlack;
            int distanceToWhite;

            if (foundBlack)
            {
                distanceToBlack = default;
                distanceToWhite = start;
            }
            else
            {
                distanceToWhite = default;
                distanceToBlack = start;
            }

            // Loop over the relevant rings
            for(int i = start; i < end; i++)
            {
                (int Dx, int Dy)[] ring = m_rings[i];

                // Loop over all the deltas
                for(int j = default; j < ring.Length; j++)
                {
                    var (dx, dy) = ring[j];
                    int newX = x + dx;
                    if (newX >= default(int) && newX < width)
                    {
                        int newY = y + dy;
                        if (newY >= default(int) && newY < height)
                        {
                            // Check to see if we found what we are looking for
                            if (sourceArray[newX, newY])
                            {
                                foundBlack = true;
                            }
                            else
                            {
                                foundWhite = true;
                            }

                            // Now look for exit conditions... Did we find all the stuff we are looking for?
                            if (foundBlack && foundWhite)
                            {
                                return (distanceToBlack, distanceToWhite);
                            }
                        }
                    }
                }

                // If we have not found what we are looking keep bumping up the counts once for each ring
                // This may be a bit of lie if we really get far away, but it for what we are doing that should be ok
                if (!foundBlack)
                {
                    distanceToBlack++;
                }
                if (!foundWhite)
                {
                    distanceToWhite++;
                }
            }

            // If we got this far at lest one of these have to be out of range.
            if (!foundBlack)
            {
                distanceToBlack += m_outOfRangeCost;
            }
            if (!foundWhite)
            {
                distanceToWhite += m_outOfRangeCost;
            }

            return (distanceToBlack, distanceToWhite);
        }

        // Our rings will go from [0 ... MaxRingRadius + 1][0 ... Number of items in that ring -1]
        private readonly (int Dx, int Dy)[][] m_rings;
        private readonly int m_outOfRangeCost;

        private readonly int m_maxRingRadius;

        private const int c_neighborRange = 1;
    }
}