using System;
using Voxel.World;

namespace Voxel.Saving
{
    [Serializable]
    public class ChunkSaveData
    {
        public BlockType[,,] BlockTypeData { get; }

        public ChunkSaveData() { }

        public ChunkSaveData(BlockType[,,] blockTypeData)
        {
            BlockTypeData = blockTypeData;
        }
    }
}