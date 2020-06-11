using System.Collections;
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
        private float waterPhysicsLoopTime = 0.75f;

        protected override void Awake()
        {
            base.Awake();
            waterPhysicsLoopWFS = new WaitForSeconds(waterPhysicsLoopTime);
        }

        public void StartWaterPhysicsLoop(Block block) => StartCoroutine(WaterPhysicsLoopCoroutine(block));

        private IEnumerator WaterPhysicsLoopCoroutine(Block block)
        {
            Block currentBlock = block.GetBlockNeighbour(Neighbour.Bottom);
            do
            {
                currentBlock.UpdateBlockAndChunk(BlockType.Fluid);
                currentBlock = currentBlock.GetBlockNeighbour(Neighbour.Bottom);
                yield return waterPhysicsLoopWFS;
            } while (currentBlock?.BlockType == BlockType.Air);
        }
    }
}