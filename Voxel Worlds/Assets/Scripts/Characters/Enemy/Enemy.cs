using System.Collections;
using UnityEngine;
using Voxel.World;

namespace Voxel.Characters.Enemy
{
    public enum EnemyType
    {
        Spider
    }

    public class Enemy : Character
    {
        [SerializeField]
        private EnemyType type = default;
        public EnemyType Type => type;

        public int Health { get; set; }
        public const int StartingHealth = 100;

        public Chunk CurrentChunk { get; set; }

        private void Awake() => Health = StartingHealth;

        private void OnEnable() => StartCoroutine(ChunkSaveUpdateLoop());

        private IEnumerator ChunkSaveUpdateLoop()
        {
            while (true)
            {
                Chunk chunk = WorldManager.Instance.GetChunkFromWorldPosition(transform.position);
                if (CurrentChunk == null)
                {
                    CurrentChunk = chunk;
                }

                if (chunk?.BlockGameObject != null
                    && CurrentChunk?.BlockGameObject != null
                    && !Mathf.Approximately(chunk.BlockGameObject.transform.position.sqrMagnitude,
                                            CurrentChunk.BlockGameObject.transform.position.sqrMagnitude))
                {
                    CurrentChunk.Enemies.Remove(this);
                    CurrentChunk = chunk;

                    #if UNITY_EDITOR
                    name = $"{Type}_{CurrentChunk?.BlockGameObject.transform.position}";
                    #endif

                    CurrentChunk.Enemies.Add(this);
                }

                yield return EnemySpawner.Instance.EnemyChunkSaveUpdateLoop;
            }
        }

        private void OnDisable() => StopCoroutine(ChunkSaveUpdateLoop());
    }
}