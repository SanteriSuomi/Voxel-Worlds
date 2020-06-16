
using UnityEngine;

namespace Voxel.World
{
    public class Tree
    {
        private const int treeStemLength = 3;
        private const int treeLeavesLengthX = 2;
        private const int treeLeavesLengthY = 1;
        private const int treeLeavesLengthZ = 2;

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
            for (int i = 0; i < treeStemLength; i++)
            {
                Block.UpdateBlockType(BlockType.Wood);
                Block = Block.GetBlockNeighbour(Neighbour.Top);
            }
        }

        private void TreeLeaves()
        {
            Vector3Int position = Block.Position;
            for (int x = -treeLeavesLengthX; x < treeLeavesLengthX; x++)
            {
                for (int y = -treeLeavesLengthY; y < treeLeavesLengthY; y++)
                {
                    for (int z = -treeLeavesLengthZ; z < treeLeavesLengthZ; z++)
                    {
                        LeafifyBlock(Block);
                        Block newBlock = Block.GetBlock(position.x + x, position.y + y, position.z + z);
                        if (newBlock != null)
                        {
                            Block = newBlock;
                        }
                    }
                }
            }
        }

        private void LeafifyBlock(Block block)
        {
            if (block.BlockType == BlockType.Air)
            {
                block.UpdateBlockType(BlockType.Leaf);
            }
        }
    }
}