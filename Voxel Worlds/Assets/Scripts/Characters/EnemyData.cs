using System;
using UnityEngine;
using Voxel.Characters.Enemy;

namespace Voxel.Characters.Saving
{
    [Serializable]
    public class EnemyData : CharacterData
    {
        public EnemyType Type { get; }
        public float Health { get; }

        public EnemyData(EnemyType type, float health, Vector3 position, Quaternion rotation)
                        : base(position, rotation)
        {
            Type = type;
            Health = health;
        }
    }
}