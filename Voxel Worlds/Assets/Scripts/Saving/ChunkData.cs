using System;
using UnityEngine;
using Voxel.World;

namespace Voxel.Saving
{
    [Serializable]
    public class ChunkData
    {
        public BlockType[,,] BlockTypeData { get; }

        // Vector position
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public ChunkData() { }

        public ChunkData(BlockType[,,] blockTypeData, Vector3 chunkPosition)
        {
            X = chunkPosition.x;
            Y = chunkPosition.y;
            Z = chunkPosition.z;

            BlockTypeData = blockTypeData;
        }
    }
}