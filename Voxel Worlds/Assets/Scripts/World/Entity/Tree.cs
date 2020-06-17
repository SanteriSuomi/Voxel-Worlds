using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Voxel.World
{
    public class Tree
    {
        private const int treeStemLengthBase = 4;

        private const int treeLeavesSize = 2;

        // 0 for no variation
        private const int treeStemLengthVariation = 1;
        private const int treeLeavesSizeVariation = 0;

        public Block[,,] Blocks { get; }
        public Block Block { get; private set; }

        public Tree(Block[,,] chunkBlocks, Block initialBlock)
        {
            Blocks = chunkBlocks;
            Block = initialBlock;
        }

        public void GenerateTree()
        {
            TreeStem();
            TreeLeaves();
        }

        private void TreeStem()
        {
            int length = treeStemLengthBase + Random.Range(0, treeStemLengthVariation + 1);
            for (int i = 0; i < length; i++)
            {
                Block.UpdateBlockType(BlockType.Wood);
                Block = Block.GetBlockNeighbour(Neighbour.Top);
            }
        }

        private void TreeLeaves()
        {
            List<Block> sideNeighbours = new List<Block>();
            int size = treeLeavesSize + Random.Range(0, treeLeavesSizeVariation + 1);
            AddSides(sideNeighbours, Neighbour.Left, size);
            AddSides(sideNeighbours, Neighbour.Right, size);
            AddSides(sideNeighbours, Neighbour.Back, size);
            AddSides(sideNeighbours, Neighbour.Front, size);
            AddSides(sideNeighbours, Neighbour.Top, size);

            for (int i = 0; i < sideNeighbours.Count; i++)
            {
                Block sideNeighbour = sideNeighbours[i];
                if (sideNeighbour != null)
                {
                    var sideNeighbourNeighbours = sideNeighbour.GetAllBlockNeighbours().Values;
                    for (int j = 0; j < sideNeighbourNeighbours.Count; j++)
                    {
                        Block sideNeighboursNeighbour = sideNeighbourNeighbours.ElementAt(j);
                        Leafify(sideNeighboursNeighbour);
                    }
                }
            }
        }

        private void AddSides(List<Block> sideNeighbours, Neighbour neighbour, int size)
        {
            Block block = Block;
            for (int i = 0; i < size; i++)
            {
                Block newBlock = block.GetBlockNeighbour(neighbour);
                if (newBlock != null)
                {
                    block = newBlock;
                    sideNeighbours.Add(block);
                }
            }
        }

        private static void Leafify(Block block)
        {
            if (block?.BlockType == BlockType.Air)
            {
                block.UpdateBlockType(BlockType.Leaf);
            }
        }
    }
}