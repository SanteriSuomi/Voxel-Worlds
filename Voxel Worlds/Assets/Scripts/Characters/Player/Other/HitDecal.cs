using System.Collections;
using UnityEngine;
using Voxel.Utility.Pooling;
using Voxel.World;

namespace Voxel.Other
{
    public class HitDecal : MonoBehaviour
    {
        [SerializeField]
        private int maxTimeAlive = 5;
        private string currentDecalDatabaseKey;

        public void Activate(Block block, string databaseKey)
        {
            transform.position = block.BlockPositionAverage;
            currentDecalDatabaseKey = databaseKey;
            WorldManager.Instance.HitDecalDatabase.TryAdd(currentDecalDatabaseKey, this);
            StartCoroutine(DecalCoroutine(block));
        }

        private IEnumerator DecalCoroutine(Block block)
        {
            BlockType blockTypeAtStart = block.BlockType;
            float aliveTimer = 0;
            while (aliveTimer < maxTimeAlive)
            {
                if (block == null
                    || block.BlockType != blockTypeAtStart)
                {
                    // If block gets destroyed/modified.
                    Deactivate(block, false);
                    yield break;
                }

                aliveTimer += Time.deltaTime;
                yield return null;
            }

            Deactivate(block, true);
        }

        public void Deactivate(Block block, bool resetBlockHealth)
        {
            if (resetBlockHealth)
            {
                block.ResetBlockHealth();
            }
            
            WorldManager.Instance.HitDecalDatabase.TryRemove(currentDecalDatabaseKey, out _);
            HitDecalPool.Instance.Return(this);
        }
    }
}