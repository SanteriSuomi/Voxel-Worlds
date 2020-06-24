using System;
using System.Collections;
using UnityEngine;
using Voxel.Utility;
using Voxel.World;
using Random = UnityEngine.Random;

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

        public Enemy GetRandomEnemy() => enemies[Random.Range(0, enemies.Length)];
    }

    public struct EnemySpawnData
    {
        public EnemyType Type { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public float Health { get; }
        public Chunk Chunk { get; }

        public EnemySpawnData(EnemyType type, Vector3 position, Quaternion rotation, float health, Chunk chunk)
        {
            Type = type;
            Position = position;
            Rotation = rotation;
            Health = health;
            Chunk = chunk;
        }
    }

    public class EnemyManager : Singleton<EnemyManager>
    {
        [SerializeField, Tooltip("Bigger value means smaller chance. e.g 2500 means 1 in 2500 chance, for every top block.")]
        private int enemySpawnChance = 2500;
        public int EnemySpawnChance => enemySpawnChance;

        [SerializeField]
        private Vector3 enemySpawnOffset = new Vector3(0, 3, 0);
        public Vector3 EnemySpawnOffset => enemySpawnOffset;

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
        private float enemyActivateDelay = 5;
        private WaitForSeconds enemyActivateDelayWFS;

        [SerializeField]
        private Enemies enemies = default;
        public Enemies Enemies => enemies;

        protected override void Awake()
        {
            base.Awake();
            enemyActivateDelayWFS = new WaitForSeconds(enemyActivateDelay);
        }

        /// <summary>
        /// Spawn an enemy with the provided data.
        /// </summary>
        /// <param name="data">Data which with the enemy construction happens.</param>
        /// <returns>The enemy class.</returns>
        public Enemy Spawn(EnemySpawnData data)
        {
            Enemy enemy = Instantiate(enemies.GetEnemy(data.Type), data.Position, data.Rotation);
            enemy.gameObject.SetActive(false);
            enemy.CurrentChunk = data.Chunk;
            enemy.CurrentChunk.Enemies.Add(enemy);
            enemy.name = $"{data.Type}_{data.Position}";
            enemy.Health = data.Health;
            enemy.transform.position = data.Position;
            return enemy;
        }

        public void ActivateEnemyDelay(Component obj) => StartCoroutine(ActivateEnemyDelayCoroutine(obj));

        private IEnumerator ActivateEnemyDelayCoroutine(Component obj)
        {
            yield return enemyActivateDelayWFS;
            obj.gameObject.SetActive(true);
        }
    }
}