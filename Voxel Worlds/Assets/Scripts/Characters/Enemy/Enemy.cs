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

                if (chunk != CurrentChunk)
                {
                    CurrentChunk.Enemies.Remove(this);
                    CurrentChunk = chunk;
                    Debug.Log(CurrentChunk);
                    #if UNITY_EDITOR
                    gameObject.name = $"{gameObject.name}_{CurrentChunk?.BlockGameObject.transform.position}";
                    #endif
                    CurrentChunk.Enemies.Add(this);
                }

                yield return EnemySpawner.Instance.EnemyChunkSaveUpdateLoop;
            }
        }

        private void OnDisable() => StopCoroutine(ChunkSaveUpdateLoop());
    }
}