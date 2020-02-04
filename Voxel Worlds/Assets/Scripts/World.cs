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
        /// Return the ID of the chunk at position as specified in the chunkdictionary
        /// </summary>
        /// <param name="atPosition"></param>
        /// <returns></returns>
        public static string GetChunkID(Vector3 atPosition)
        {
            return $"{(int)atPosition.x} {(int)atPosition.y} {(int)atPosition.z}";
        }

        [SerializeField]
        private int chunkColumnHeight = 8;
        public int ChunkColumnHeight { get; private set; }
        [SerializeField]
        private int chunkRowLength = 8;
        public int ChunkRowLength { get; private set; }
        [SerializeField]
        private int chunkDepthLength = 8;
        public int ChunkDepthLength { get; private set; }
        [SerializeField]
        private int chunkSize = 16;
        public int ChunkSize { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            ChunkColumnHeight = chunkColumnHeight;
            ChunkRowLength = chunkRowLength;
            ChunkDepthLength = chunkDepthLength;
            ChunkSize = chunkSize;
            StartCoroutine(BuildChunks());
        }

        private IEnumerator BuildChunks()
        {
            for (int column = 0; column < ChunkColumnHeight; column++)
            {
                for (int row = 0; row < ChunkRowLength; row++)
                {
                    for (int depth = 0; depth < ChunkDepthLength; depth++)
                    {
                        Vector3 chunkPosition = new Vector3(column * ChunkSize, row * ChunkSize, depth * ChunkSize);
                        Chunk chunk = new Chunk(chunkPosition, worldTextureAtlas, transform);
                        ChunkDictionary.Add(chunk.ChunkName, chunk);
                    }
                }
            }

            foreach (KeyValuePair<string, Chunk> chunk in ChunkDictionary)
            {
                chunk.Value.BuildChunk();
            }

            yield return null;
        }
    }
}