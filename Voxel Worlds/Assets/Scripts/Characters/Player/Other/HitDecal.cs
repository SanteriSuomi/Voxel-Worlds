using System.Collections;
using UnityEngine;
using Voxel.Utility.Pooling;
using Voxel.World;

namespace Voxel.Other
{
    public class HitDecal : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] decals = default;

        private const int maxTimeAlive = 5;
        private string currentDecalDatabaseKey;

        public void Activate(Block block, string databaseKey)
        {
            transform.position = block.BlockPositionAverage;
            currentDecalDatabaseKey = databaseKey;
            WorldManager.Instance.HitDecalDatabase.TryAdd(currentDecalDatabaseKey, this);
            decals[0].SetActive(true); // Activate the first decal (smallest breakage)
            StartCoroutine(DecalCoroutine(block));
        }

        private IEnumerator DecalCoroutine(Block block)
        {
            BlockType blockTypeAtStart = block.BlockType;

            float aliveTimer = 0;
            while (aliveTimer < maxTimeAlive)
            {
                DetermineDecal(block);

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

        private void DetermineDecal(Block block)
        {
            if (block.BlockHealth == block.MidBlockHealth)
            {
                decals[decals.Length / 2].SetActive(true);
            }
            else if (block.BlockHealth == block.MinBlockHealth)
            {
                decals[decals.Length - 1].SetActive(true);
            }
        }

        public void Deactivate(Block block, bool resetBlockHealth)
        {
            if (resetBlockHealth)
            {
                block.ResetBlockHealth();
            }

            for (int i = 0; i < decals.Length; i++)
            {
                decals[i].SetActive(false);
            }

            WorldManager.Instance.HitDecalDatabase.TryRemove(currentDecalDatabaseKey, out _);
            HitDecalPool.Instance.Return(this);
        }
    }
}