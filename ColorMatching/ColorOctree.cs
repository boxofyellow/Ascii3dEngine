using System;
using System.Runtime.CompilerServices;
using SixLabors.ImageSharp.PixelFormats;

namespace Ascii3dEngine
{
    // An implementation of a https://en.wikipedia.org/wiki/Octree to aid in finding similar colors
    // One difference here is what each node will only have 8 child nodes, it can have maxChildrenCount leafs
    //
    // I benched marked sealing these and found no real saving, Some even turned up worse.  Those were likely just transient variations 
    public class ColorOctree
    {
        public ColorOctree(int maxChildrenCount) => m_root = new ColorOctreeNode(maxChildrenCount,
            byte.MinValue, byte.MaxValue,
            byte.MinValue, byte.MaxValue,
            byte.MinValue, byte.MaxValue);

        public void Add(ColorOctreeLeaf leaf) => m_root.Add(leaf);

        public (int NodeCount, int LeafCount, int NodesWithLeafs) Count() => m_root.Count();

        public (Char Character, ConsoleColor Foreground, ConsoleColor Background, Rgb24 Result) BestMatch(Rgb24 target) => m_root.BestMatch(target);

        private readonly ColorOctreeNode m_root;
    }

    public class ColorOctreeNode
    {
        public ColorOctreeNode(int maxChildrenCount, byte minR, byte maxR, byte minG, byte maxG, byte minB, byte maxB)
        {
            MinR = minR;
            MaxR = maxR;
            MinG = minG;
            MaxG = maxG;
            MinB = minB;
            MaxB = maxB;
            m_maxChildrenCount = maxChildrenCount;

            // All noes will get some leafs, even if they later get converted into inner nodes
            Leafs = new ColorOctreeLeaf[m_maxChildrenCount];

            // These will not get used in "true" leaf nodes, but these arrays are small, but should review if that should be optimized
            Nodes = new ColorOctreeNode[c_maxNodeChildCount];

            m_widthR = (byte)((MaxR - MinR)/2);
            m_widthG = (byte)((MaxG - MinG)/2);
            m_widthB = (byte)((MaxB - MinB)/2);

            m_midR = (byte)(MaxR - m_widthR);
            m_midG = (byte)(MaxG - m_widthG);
            m_midB = (byte)(MaxB - m_widthB);
        }

        public void Add(ColorOctreeLeaf leaf)
        {
            if (LeafChildCount == m_maxChildrenCount)
            {
                Split();
            }

            if (LeafChildCount < 0)
            {
                Nodes[NodeIndex(leaf.Color)].Add(leaf);
            }
            else
            {
#if DEBUG
                CheckBounds(leaf.Color);
#endif
                Leafs[LeafChildCount] = leaf;
                LeafChildCount++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int NodeIndex(Rgb24 color)
        {
#if DEBUG
            CheckBounds(color);
#endif
            return (color.R < m_midR ? 0 : c_offSetR)
                 + (color.G < m_midG ? 0 : c_offSetG)
                 + (color.B < m_midB ? 0 : c_offSetB);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckBounds(Rgb24 color)
        {
            if (color.R < MinR
                || color.G < MinG
                || color.B < MinB
                || color.R > MaxR
                || color.G > MaxG
                || color.B > MaxB)
            {
                throw new InvalidOperationException($"{color} does not belong here in {this}");
            }
        }

        public (Char Character, ConsoleColor Foreground, ConsoleColor Background, Rgb24 Result) BestMatch(Rgb24 target)
        {
            ColorOctreeNode current = this;
            ColorOctreeNode? oneBack = null;

            while (current.LeafChildCount < 0)
            {
                int index = current.NodeIndex(target);
                oneBack = current;
                current = current.Nodes[index];

                if (current == null)
                {
                    throw new NullReferenceException($"Found a null node in {oneBack} at {index}");
                }
            }

            int resultDistanceProxy = int.MaxValue;
            char character = default;
            ConsoleColor foreground = default;
            ConsoleColor background = default;
            Rgb24 result = default;

            if (current.LeafChildCount == 0)
            {
                // if this has no leafs then we will go back up one and check all the leafs we can find under it
                if (oneBack == null)
                {
                    throw new NullReferenceException($"{nameof(oneBack)} is null in {current}");
                }

                // I was hopeing to do this without recursion but that does not look really viable here since we need to check ALL the leafs under onBack
                // regardless of where they are are in the tree. 
                CheckNodes(oneBack);
            }
            else
            {
                CheckLeafs(current);
            }

            return (character, foreground, background, result);

            void CheckNodes(ColorOctreeNode node)
            {
                if (node.LeafChildCount < 0)
                {
                    for (int i = 0; i < node.Nodes.Length; i++)
                    {
                        CheckNodes(node.Nodes[i]);
                    }
                }
                else
                {
                    CheckLeafs(node);
                }
            }

            void CheckLeafs(ColorOctreeNode node)
            {
                for (int i = 0; i < node.LeafChildCount; i++)
                {
                    var color = node.Leafs[i];
                    int distanceProxy = ColorUtilities.DifferenceProxy(target, color.Color);
                    if (distanceProxy < resultDistanceProxy)
                    {
                        character = color.Character;
                        foreground = color.Foreground;
                        background = color.Background;
                        resultDistanceProxy = distanceProxy;
                        result = color.Color;
                    }
                }
            }
        }

        public (int NodeCount, int LeafCount, int NodesWithLeafs) Count()
        {
            if (LeafChildCount < 0)
            {
                int nodeCount = 1; // For this
                int leafCount = 0;
                int nodesWithLeafs = 0;
                for (int i = 0; i < Nodes.Length; i++)
                {
                    var x = Nodes[i].Count();
                    nodeCount += x.NodeCount;
                    leafCount += x.LeafCount;
                    nodesWithLeafs += x.NodesWithLeafs;
                }
                return (nodeCount, leafCount, nodesWithLeafs);
            }
            // 1 For this;
            return (1, LeafChildCount, 1);
        }

        public int LeafChildCount { get; private set; }
        public readonly byte MinR;
        public readonly byte MaxR;
        public readonly byte MinG;
        public readonly byte MaxG;
        public readonly byte MinB;
        public readonly byte MaxB;
        public readonly ColorOctreeLeaf[] Leafs;
        public readonly ColorOctreeNode[] Nodes;

        public override string ToString() => $"(iR:{MinR}, aR:{MaxR}, iG:{MinG}, aG:{MaxG}, iB:{MinB}, aR:{MaxB}, LC:{LeafChildCount})";

        private void Split()
        {
            // Create all the child Nodes;
            for (int b = 0; b < 2; b++)
            {
                byte minB = b == 0 ? MinB : m_midB;
                byte maxB = b == 0 ? (byte)(MinB + m_widthB) : MaxB;
                int indexB = b * c_offSetB;
                for (int g = 0; g < 2; g++)
                {
                    byte minG = g == 0 ? MinG : m_midG;
                    byte maxG = g == 0 ? (byte)(MinG + m_widthG) : MaxG;
                    int indexG = g * c_offSetG;
                    for (int r = 0; r < 2; r++)
                    {
                        byte minR = r == 0 ? MinR : m_midR;
                        byte maxR = r == 0 ? (byte)(MinR + m_widthR) : MaxR;

                        int index = r + indexG + indexB;
                        Nodes[index] = new ColorOctreeNode(m_maxChildrenCount, minR, maxR, minG, maxG, minB, maxB);
                    }
                }
            }

            // Mark this as split, and re-add all the Leafs so they will get distributed into the children
            LeafChildCount =-1;
            for (int i = 0; i < Leafs.Length; i++)
            {
                Add(Leafs[i]);
            }
        }

        private readonly byte m_widthR;
        private readonly byte m_widthG;
        private readonly byte m_widthB;

        private readonly byte m_midR;
        private readonly byte m_midG;
        private readonly byte m_midB;

        private readonly int m_maxChildrenCount;

        private const int c_maxNodeChildCount = 8; // Its not called an OCTree for nothing :)


        private const int c_offSetR = 1;
        private const int c_offSetG = c_offSetR * 2;
        private const int c_offSetB = c_offSetG * 2;

    }

    public struct ColorOctreeLeaf
    {
        public ColorOctreeLeaf(ConsoleColor foreground, ConsoleColor background, char character, Rgb24 color)
        {
            Foreground = foreground;
            Background = background;
            Character = character;
            Color = color;
        }

        public readonly ConsoleColor Foreground;
        public readonly ConsoleColor Background;
        public readonly char Character;
        public readonly Rgb24 Color;

        public override string ToString() => $"(F:{Foreground}, B:{Background}, C:{Character}, {Color})";
    }
}