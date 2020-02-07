using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voxel.Utility;

namespace Voxel.World
{
    public class World : Singleton<World>
    {
        [SerializeField]
        private Material worldTextureAtlas = default;
        public Dictionary<string, Chunk> ChunkDictionary { get; } = new Dictionary<string, Chunk>();

        /// <summary>
        /// Return the ID of a chunk at position as specified in the ChunkDictionary.
        /// </summary>
        /// <param name="atPosition"></param>
        /// <returns></returns>
        public static string GetChunkID(Vector3 atPosition)
        {
            return $"{(int)atPosition.x} {(int)atPosition.y} {(int)atPosition.z}";
        }

        [SerializeField]
        private int chunkColumnLength = 8;
        [SerializeField]
        private int chunkRowHeight = 8;
        [SerializeField]
        private int chunkDepthLength = 8;
        [SerializeField]
        private int chunkSize = 16;
        public int ChunkSize { get; private set; }
        public int MaxWorldHeight { get { return chunkRowHeight * ChunkSize; } }

        protected override void Awake()
        {
            base.Awake();
            ChunkSize = chunkSize;
            StartCoroutine(BuildWorld());
        }

        private IEnumerator BuildWorld()
        {
            // Initialise chunks
            for (int x = 0; x < chunkColumnLength; x++)
            {
                for (int y = 0; y < chunkRowHeight; y++)
                {
                    for (int z = 0; z < chunkDepthLength; z++)
                    {
                        Vector3 chunkPosition = new Vector3(x * ChunkSize, y * ChunkSize, z * ChunkSize);
                        Chunk chunk = new Chunk(chunkPosition, worldTextureAtlas, transform);
                        ChunkDictionary.Add(GetChunkID(chunkPosition), chunk);
                    }
                }
            }

            // Build initialised chunks
            foreach (KeyValuePair<string, Chunk> chunk in ChunkDictionary)
            {
                chunk.Value.BuildChunk();
            }

            // Build chunk blocks
            foreach (KeyValuePair<string, Chunk> chunk in ChunkDictionary)
            {
                chunk.Value.BuildChunkBlocks();
            }

            yield return null;
        }
    }
}