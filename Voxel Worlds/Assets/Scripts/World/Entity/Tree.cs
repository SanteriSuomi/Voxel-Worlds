using UnityEngine;

namespace Voxel.World
{
    public class Tree
    {
        private const int treeStemLength = 3;
        private const int treeLeavesLength = 3;

        public Chunk Chunk { get; }
        public Block[,,] Blocks { get; }

        public Tree(Chunk chunkOwner, Block[,,] chunkBlocks, Block initialBlock)
        {
            Chunk = chunkOwner;
            Blocks = chunkBlocks;
            GenerateTree(initialBlock);
        }

        private void GenerateTree(Block initialBlock)
        {
            initialBlock = TreeStem(initialBlock);
            TreeLeaves(initialBlock);
        }

        private static Block TreeStem(Block initialBlock)
        {
            for (int i = 0; i < treeStemLength; i++)
            {
                initialBlock.UpdateBlockType(BlockType.Wood);
                Block newBlock = initialBlock.GetBlockNeighbour(Neighbour.Top);
                if (newBlock != null)
                {
                    initialBlock = newBlock;
                    continue;
                }

                break;
            }

            return initialBlock;
        }

        private void TreeLeaves(Block initialBlock)
        {
            Vector3Int position = initialBlock.Position;
            for (int x = -treeLeavesLength; x < treeLeavesLength; x++)
            {
                for (int y = -treeLeavesLength; y < treeLeavesLength; y++)
                {
                    for (int z = -treeLeavesLength; z < treeLeavesLength; z++)
                    {
                        LeafifyBlock(initialBlock);
                        Block newLeafInitialBlock = initialBlock.GetBlock(position.x + x, position.y + y, position.z + z);
                        if (newLeafInitialBlock != null)
                        {
                            initialBlock = newLeafInitialBlock;
                            continue;
                        }

                        break;
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