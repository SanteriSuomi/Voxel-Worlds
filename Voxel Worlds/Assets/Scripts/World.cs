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
        private int chunkColumnHeight = 8;
        [SerializeField]
        private int chunkRowLength = 8;
        [SerializeField]
        private int chunkDepthLength = 8;
        [SerializeField]
        private int chunkSize = 16;
        public int ChunkSize { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            ChunkSize = chunkSize;
            StartCoroutine(BuildChunks());
        }

        private IEnumerator BuildChunks()
        {
            for (int x = 0; x < chunkColumnHeight; x++)
            {
                for (int y = 0; y < chunkRowLength; y++)
                {
                    for (int z = 0; z < chunkDepthLength; z++)
                    {
                        Vector3 chunkPosition = new Vector3(x * ChunkSize, y * ChunkSize, z * ChunkSize);
                        Chunk chunk = new Chunk(chunkPosition, worldTextureAtlas, transform);
                        ChunkDictionary.Add(chunk.ChunkName, chunk);
                        chunk.BuildChunk();
                    }
                }
            }

            //foreach (KeyValuePair<string, Chunk> chunk in ChunkDictionary)
            //{
            //    chunk.Value.BuildChunk();
            //}

            yield return null;
        }
    }
}