using System.Collections;
using UnityEngine;
using Voxel.Utility.Pooling;
using Voxel.World;

namespace Voxel.Other
{
    public class HitDecal : MonoBehaviour
    {
        private const int maxTimeAlive = 5;

        [SerializeField]
        private GameObject[] decals = default;
        private GameObject currentlyActiveDecal;

        private string currentDecalDatabaseKey;

        public void Activate(Block block, string databaseKey)
        {
            transform.position = block.WorldPositionAverage;
            currentDecalDatabaseKey = databaseKey;
            WorldManager.Instance.HitDecalDatabase.TryAdd(currentDecalDatabaseKey, this);
            ActivateNewDecal(decals[0]);
            StartCoroutine(HitDecalCoroutine(block));
        }

        private IEnumerator HitDecalCoroutine(Block block)
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
                ActivateNewDecal(decals[decals.Length / 2]);
            }
            else if (block.BlockHealth == block.MinBlockHealth)
            {
                ActivateNewDecal(decals[decals.Length - 1]);
            }
        }

        private void ActivateNewDecal(GameObject decal)
        {
            if (currentlyActiveDecal != null)
            {
                currentlyActiveDecal.SetActive(false);
            }
            
            currentlyActiveDecal = decal;
            currentlyActiveDecal.SetActive(true);
        }

        public void Deactivate(Block block, bool resetBlockHealth)
        {
            if (resetBlockHealth)
            {
                block.ResetBlockHealth();
            }

            currentlyActiveDecal.SetActive(false);

            WorldManager.Instance.HitDecalDatabase.TryRemove(currentDecalDatabaseKey, out _);
            HitDecalPool.Instance.Return(this);
        }
    }
}