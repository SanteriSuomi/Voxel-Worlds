using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxel.Utility;

namespace Voxel.World
{
    /// <summary>
    /// Global MonoBehaviour for chunks to start coroutines.
    /// </summary>
    public class GlobalChunk : Singleton<GlobalChunk>
    {
        private WaitForSeconds waterPhysicsLoopWFS;
        [SerializeField]
        private float waterPhysicsLoop = 0.75f;
        [SerializeField]
        private int maxWaterExpansion = 25;

        protected override void Awake()
        {
            base.Awake();
            waterPhysicsLoopWFS = new WaitForSeconds(waterPhysicsLoop);
        }

        #region Water Dynamics
        public void StartWaterDynamic(Block block) => StartCoroutine(WaterDynamicDown(block));

        private IEnumerator WaterDynamicDown(Block block)
        {
            Block currentBlock = block.GetBlockNeighbour(Neighbour.Bottom);
            while (currentBlock?.BlockType == BlockType.Air)
            {
                currentBlock.UpdateBlockAndChunk(BlockType.Fluid);
                currentBlock = currentBlock.GetBlockNeighbour(Neighbour.Bottom);
                yield return waterPhysicsLoopWFS;
            }

            if (currentBlock != null)
            {
                StartCoroutine(WaterDynamicNeighbours(currentBlock.GetBlockNeighbour(Neighbour.Top), new RefInt(0)));
            }
        }

        private IEnumerator WaterDynamicNeighbours(Block block, RefInt counter)
        {
            if (counter.Value >= maxWaterExpansion) yield break;

            Dictionary<Neighbour, Block> blocks = block.GetAllBlockNeighbours();
            for (int i = 0; i < blocks.Count; i++)
            {
                KeyValuePair<Neighbour, Block> element = blocks.ElementAt(i);
                if (element.Value.BlockType == BlockType.Air
                    && element.Key != Neighbour.Top
                    && element.Key != Neighbour.Bottom)
                {
                    counter.Value++;
                    element.Value.UpdateBlockAndChunk(BlockType.Fluid);
                    StartCoroutine(WaterDynamicNeighbours(element.Value, counter));
                }

                yield return waterPhysicsLoopWFS;
            }
        }
        #endregion
    }
}