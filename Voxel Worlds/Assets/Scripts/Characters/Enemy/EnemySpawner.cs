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

        public void Spawn(EnemyType type, Vector3 position)
        {
            Enemy instantiatedEnemy = Instantiate(enemies.GetEnemy(type), position, Quaternion.identity);
            instantiatedEnemy.transform.position = position;
        }
    }
}