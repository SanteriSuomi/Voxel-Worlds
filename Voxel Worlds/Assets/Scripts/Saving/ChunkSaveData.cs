using System;
using Voxel.Characters.Saving;
using Voxel.World;

namespace Voxel.Saving
{
    [Serializable]
    public class ChunkSaveData
    {
        public BlockType[,,] BlockTypeData { get; }
        public bool TreesCreated { get; }
        public CharacterData[] Enemies { get; }

        public ChunkSaveData() { }

        public ChunkSaveData(BlockType[,,] blockTypeData, bool treesCreated, CharacterData[] enemies)
        {
            BlockTypeData = blockTypeData;
            TreesCreated = treesCreated;
            Enemies = enemies;
        }
    }
}