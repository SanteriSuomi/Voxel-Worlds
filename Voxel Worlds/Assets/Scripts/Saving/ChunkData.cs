using System;
using UnityEngine;
using Voxel.World;

namespace Voxel.Saving
{
    [Serializable]
    public class ChunkData
    {
        public BlockType[,,] BlockTypeData { get; }
        public Vector3 Position { get; }

        public ChunkData() { }

        public ChunkData(BlockType[,,] blockTypeData, Vector3 chunkPosition)
        {
            BlockTypeData = blockTypeData;
            Position = chunkPosition;
        }
    }
}