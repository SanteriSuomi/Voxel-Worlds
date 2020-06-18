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

        private Chunk currentChunk;

        private void Start()
        {
            currentChunk = WorldManager.Instance.GetChunkFromWorldPosition(transform.position);
            currentChunk.Enemies.Add(this);
            StartCoroutine(ChunkSaveUpdateLoop());
        }

        private IEnumerator ChunkSaveUpdateLoop()
        {
            while (enabled)
            {
                Chunk chunk = WorldManager.Instance.GetChunkFromWorldPosition(transform.position);
                Debug.Log(chunk.BlockGameObject.transform.position);
                Debug.Log(currentChunk.BlockGameObject.transform.position);
                if (chunk != currentChunk)
                {
                    currentChunk.Enemies.Remove(this);
                    currentChunk = chunk;
                    currentChunk.Enemies.Add(this);
                }

                yield return EnemySpawner.Instance.EnemyChunkSaveUpdateLoop;
            }
        }

        private void OnDisable() => StopCoroutine(ChunkSaveUpdateLoop());
    }
}