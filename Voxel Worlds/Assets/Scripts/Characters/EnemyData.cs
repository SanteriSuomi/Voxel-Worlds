using System;
using Voxel.Characters.Enemy;

namespace Voxel.Characters.Saving
{
    [Serializable]
    public class EnemyData : CharacterData
    {
        public EnemyType Type { get; }

        public EnemyData(EnemyType type)
        {
            Type = type;
        }
    }
}