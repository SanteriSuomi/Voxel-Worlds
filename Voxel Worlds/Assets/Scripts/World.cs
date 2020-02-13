using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voxel.Player;
using Voxel.Utility;

namespace Voxel.vWorld
{
    public class World : Singleton<World>
    {
        private Dictionary<string, Chunk> chunkDictionary;

        /// <summary>
        /// Return the ID of a chunk at position as specified in the ChunkDictionary.
        /// </summary>
        /// <param name="fromPosition"></param>
        /// <returns></returns>
        public static string GetChunkID(Vector3 fromPosition)
        {
            return $"{(int)fromPosition.x} {(int)fromPosition.y} {(int)fromPosition.z}";
        }

        public Chunk GetChunk(string chunkID)
        {
            if (chunkDictionary.TryGetValue(chunkID, out Chunk chunk))
            {
                return chunk;
            }

            return null;
        }

        [Header("Misc. Dependencies")]
        [SerializeField]
        private Material worldTextureAtlas = default;
        [SerializeField]
        private Transform playerTransform = default;

        [Header("Chunk and World Settings")]
        [SerializeField]
        private int chunkRowHeight = 8;
        [SerializeField]
        private int chunkSize = 16;
        public int ChunkSize { get; private set; }
        [SerializeField]
        private int radius = 1;

        public int Radius { get { return radius; } }
        public int MaxWorldHeight { get { return chunkRowHeight * ChunkSize; } }
        public int BuildWorldProgress { get; private set; } // Used for loading bar

        protected override void Awake()
        {
            base.Awake();
            chunkDictionary = new Dictionary<string, Chunk>();
            ChunkSize = chunkSize;
        }

        public void StartWorldBuild()
        {
            BuildWorldProgress = 0;
            StartCoroutine(BuildWorld());
        }

        private IEnumerator BuildWorld()
        {
            int playerPositionX = Mathf.FloorToInt(playerTransform.position.x / ChunkSize);
            int playerPositionZ = Mathf.FloorToInt(playerTransform.position.z / ChunkSize);

            BuildWorldProgress += 10;

            // Initialise chunks around player
            for (int x = -Radius; x <= Radius; x++)
            {
                for (int z = -Radius; z <= Radius; z++)
                {
                    for (int y = 0; y < chunkRowHeight; y++)
                    {


                        Vector3 chunkPosition = new Vector3((x + playerPositionX) * ChunkSize,
                                                             y * ChunkSize,
                                                            (z + playerPositionZ) * ChunkSize);

                        Chunk chunk = new Chunk(chunkPosition, worldTextureAtlas, transform);
                        chunkDictionary.Add(GetChunkID(chunkPosition), chunk);
                        yield return null;
                    }
                }
            }

            BuildWorldProgress += 30;

            // Build initialised chunks
            foreach (KeyValuePair<string, Chunk> chunk in chunkDictionary)
            {
                chunk.Value.BuildChunk();
                yield return null;
            }

            BuildWorldProgress += 30;

            // Build chunk blocks
            foreach (KeyValuePair<string, Chunk> chunk in chunkDictionary)
            {
                chunk.Value.BuildChunkBlocks();
                yield return null;
            }

            BuildWorldProgress = 100;
            
            playerTransform = PlayerManager.Instance.SpawnPlayer(MaxWorldHeight);
        }
    }
}