using System.Collections;
using UnityEngine;
using Voxel.Characters.AI;
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
        protected EnemyType type = default;
        public EnemyType Type => type;

        #region FSM/States
        [SerializeField]
        protected FSM fsm = default;
        [SerializeField]
        protected State wander = default;
        [SerializeField]
        protected State attack = default;
        [SerializeField]
        protected State defend = default;
        #endregion

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
                    UpdateSaveLocation(chunk);
                }

                yield return EnemySpawner.Instance.EnemyChunkSaveUpdateLoop;
            }
        }

        private void UpdateSaveLocation(Chunk chunk)
        {
            CurrentChunk.Enemies.Remove(this);
            CurrentChunk = chunk;

            #if UNITY_EDITOR
            name = $"{Type}_{CurrentChunk?.BlockGameObject.transform.position}";
            #endif

            CurrentChunk.Enemies.Add(this);
        }

        private void OnDisable() => StopCoroutine(ChunkSaveUpdateLoop());
    }
}