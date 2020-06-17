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
        private WaitForSeconds blockFallingDynamicInitialWFS;
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
            blockFallingDynamicInitialWFS = new WaitForSeconds(blockFallingDynamicUpdateInterval / 2);
        }

        #region Dynamics
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

        public void StartBlockFallingDynamic(Block block, BlockType blockType) => StartCoroutine(BlockFallingDown(block, blockType));

        private IEnumerator BlockFallingDown(Block block, BlockType blockType)
        {
            yield return blockFallingDynamicInitialWFS;

            List<Block> topBlocks = GetUpdateableTopBlocks(block);
            for (int i = 0; i < topBlocks.Count; i++)
            {
                Block topBlock = topBlocks[i];
                Block downBlock = topBlocks[i].GetBlockNeighbour(Neighbour.Bottom);
                do
                {
                    topBlock.UpdateBlockAndChunk(BlockType.Air);
                    downBlock.UpdateBlockAndChunk(blockType);

                    if (Mathf.Approximately(topBlock.ChunkOwner.GameObject.transform.position.sqrMagnitude,
                                            downBlock.ChunkOwner.GameObject.transform.position.sqrMagnitude))
                    {
                        topBlock.ChunkOwner.RebuildChunk(ChunkResetData.GetEmpty());
                    }
                    else
                    {
                        topBlock.ChunkOwner.RebuildChunk(ChunkResetData.GetEmpty());
                        downBlock.ChunkOwner.RebuildChunk(ChunkResetData.GetEmpty());
                    }

                    topBlock = downBlock;
                    downBlock = topBlock.GetBlockNeighbour(Neighbour.Bottom);
                    yield return blockFallingDynamicWFS;
                } while (downBlock.BlockType == BlockType.Air);
            }
        }

        private static List<Block> GetUpdateableTopBlocks(Block block)
        {
            List<Block> blocksToBeUpdated = new List<Block>();
            if (block.GetBlockNeighbour(Neighbour.Bottom).BlockType == BlockType.Air)
            {
                blocksToBeUpdated.Add(block);
            }

            Block topBlock = block.GetBlockNeighbour(Neighbour.Top);
            while (topBlock.BlockType == BlockType.Sand)
            {
                blocksToBeUpdated.Add(topBlock);
                topBlock = topBlock.GetBlockNeighbour(Neighbour.Top);
            }

            return blocksToBeUpdated;
        }
        #endregion

        //public void TreeGenerationLeaves(Block block) => StartCoroutine(TreeGenerationLeavesCoroutine(block));

        //private IEnumerator TreeGenerationLeavesCoroutine(Block block)
        //{
        //    Block[] neighbours = new Block[]
        //    {
        //        block.GetBlockNeighbour(Neighbour.Left),
        //        block.GetBlockNeighbour(Neighbour.Right),
        //        block.GetBlockNeighbour(Neighbour.Bottom),
        //        block.GetBlockNeighbour(Neighbour.Top)
        //    };

        //    for (int i = 0; i < neighbours.Length; i++)
        //    {
        //        var neighbourNeighbours = neighbours[i].GetAllBlockNeighbours().Values;
        //        for (int j = 0; j < neighbourNeighbours.Count; j++)
        //        {
        //            Block neighbour = neighbourNeighbours.ElementAt(j);
        //            if (neighbour.BlockType == BlockType.Air)
        //            {
        //                neighbour.UpdateBlockType(BlockType.Leaf);
        //            }
        //        }
        //    }

        //    yield break;
        //}
    }
}