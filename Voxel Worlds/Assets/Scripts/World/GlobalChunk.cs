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
        private WaitForSeconds waterDynamicWFS;
        private WaitForSeconds blockFallingDynamicWFS;
        [SerializeField]
        private float waterDynamicUpdateInterval = 0.75f;
        [SerializeField]
        private float blockFallingDynamicUpdateInterval = 0.375f;
        [SerializeField]
        private int maxWaterExpansion = 25;

        protected override void Awake()
        {
            base.Awake();
            waterDynamicWFS = new WaitForSeconds(waterDynamicUpdateInterval);
            blockFallingDynamicWFS = new WaitForSeconds(blockFallingDynamicUpdateInterval);
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
                yield return waterDynamicWFS;
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

                yield return waterDynamicWFS;
            }
        }
        #endregion

        public void StartBlockFallingDynamic(Block block) => StartCoroutine(BlockFallingDown(block));

        // TODO: block falling down
        private IEnumerator BlockFallingDown(Block block)
        {
            Block currentBlock = block;
            Block downBlock = currentBlock.GetBlockNeighbour(Neighbour.Bottom);
            while (downBlock.BlockType == BlockType.Air)
            {
                currentBlock.UpdateBlockType(BlockType.Air);
                downBlock.UpdateBlockType(block.BlockType);

                if (Mathf.Approximately(currentBlock.ChunkOwner.GameObject.transform.position.sqrMagnitude,
                                        downBlock.ChunkOwner.GameObject.transform.position.sqrMagnitude))
                {
                    currentBlock.ChunkOwner.RebuildChunk(ChunkResetData.GetEmpty());
                }
                else
                {
                    currentBlock.ChunkOwner.RebuildChunk(ChunkResetData.GetEmpty());
                    downBlock.ChunkOwner.RebuildChunk(ChunkResetData.GetEmpty());
                }
                
                currentBlock = downBlock;
                downBlock = currentBlock.GetBlockNeighbour(Neighbour.Bottom);
                yield return blockFallingDynamicWFS;
            }
        }
    }
}