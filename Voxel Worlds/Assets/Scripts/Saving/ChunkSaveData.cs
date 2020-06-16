using System;
using Voxel.World;

namespace Voxel.Saving
{
    [Serializable]
    public class ChunkSaveData
    {
        public BlockType[,,] BlockTypeData { get; }
        public bool TreesCreated { get; }

        public ChunkSaveData() { }

        public ChunkSaveData(BlockType[,,] blockTypeData, bool treesCreated)
        {
            BlockTypeData = blockTypeData;
            TreesCreated = treesCreated;
        }
    }
}