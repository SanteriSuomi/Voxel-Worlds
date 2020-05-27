using System;
using Voxel.World;

namespace Voxel.Saving
{
    [Serializable]
    public class ChunkData
    {
        public BlockType[,,] BlockTypeData { get; }

        public ChunkData() { }

        public ChunkData(BlockType[,,] blockTypeData)
        {
            BlockTypeData = blockTypeData;
        }
    }
}