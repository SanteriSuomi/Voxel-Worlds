using System;
using System.Collections.Generic;
using Voxel.Characters.Saving;
using Voxel.World;

namespace Voxel.Saving
{
    [Serializable]
    public class ChunkSaveData
    {
        public BlockType[,,] BlockTypeData { get; }
        public bool TreesCreated { get; }
        public List<CharacterData> Enemies { get; }

        public ChunkSaveData() { }

        public ChunkSaveData(BlockType[,,] blockTypeData, bool treesCreated, List<CharacterData> enemies)
        {
            BlockTypeData = blockTypeData;
            TreesCreated = treesCreated;
            Enemies = enemies;
        }
    }
}