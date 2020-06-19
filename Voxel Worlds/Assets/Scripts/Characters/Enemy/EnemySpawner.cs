using System;
using UnityEngine;
using Voxel.Utility;

namespace Voxel.Characters.Enemy
{
    [Serializable]
    public class Enemies
    {
        [SerializeField]
        private Enemy[] enemies = default;

        public Enemy GetEnemy(EnemyType type)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i].Type == type)
                {
                    return enemies[i];
                }
            }

            return null;
        }
    }

    public struct EnemySpawnData
    {
        public EnemyType Type { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public int Health { get; }

        public EnemySpawnData(EnemyType type, Vector3 position, Quaternion rotation, int health)
        {
            Type = type;
            Position = position;
            Rotation = rotation;
            Health = health;
        }
    }

    public class EnemySpawner : Singleton<EnemySpawner>
    {
        [SerializeField, Tooltip("Bigger value means smaller chance. e.g 2500 means 1 in 2500 chance, for every top block.")]
        private int enemySpawnChance = 2500;
        public int EnemySpawnChance => enemySpawnChance;

        [SerializeField]
        private int enemyChunkSaveUpdateLoopInterval = 1;
        private WaitForSecondsRealtime enemyChunkSaveUpdateLoopWFS;
        public WaitForSecondsRealtime EnemyChunkSaveUpdateLoop
        {
            get
            {
                if (enemyChunkSaveUpdateLoopWFS == null)
                {
                    enemyChunkSaveUpdateLoopWFS = new WaitForSecondsRealtime(enemyChunkSaveUpdateLoopInterval);
                }

                return enemyChunkSaveUpdateLoopWFS;
            }
        }

        [SerializeField]
        private Enemies enemies = default;

        /// <summary>
        /// Spawn an enemy with the provided data.
        /// </summary>
        /// <param name="data">Data which with the enemy construction happens.</param>
        /// <returns>The enemy class.</returns>
        public Enemy Spawn(EnemySpawnData data)
        {
            Enemy enemy = Instantiate(enemies.GetEnemy(data.Type), data.Position, data.Rotation);
            enemy.name = $"{enemy.name}_{data.Position}";
            enemy.Health = data.Health;
            enemy.transform.position = data.Position;
            return enemy;
        }
    }
}