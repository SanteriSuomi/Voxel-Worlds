using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using Voxel.Player;
using Voxel.Utility;

namespace Voxel.World
{
    public enum WorldStatus
    {
        None,
        Building
    }

    public class WorldManager : Singleton<WorldManager>
    {
        private Dictionary<string, Chunk> chunkDatabase;

        /// <summary>
        /// Return the ID (as a string) of a chunk at position as specified in the ChunkDictionary.
        /// </summary>
        /// <param name="fromPosition"></param>
        /// <returns></returns>
        public static string GetChunkID(Vector3 fromPosition)
        {
            return $"{(int)fromPosition.x} {(int)fromPosition.y} {(int)fromPosition.z}";
        }

        /// <summary>
        /// Get the chunk by it's ID (string) normally used with the GetChunkID method.
        /// </summary>
        /// <param name="chunkID"></param>
        /// <returns></returns>
        public Chunk GetChunk(string chunkID)
        {
            if (chunkDatabase.TryGetValue(chunkID, out Chunk chunk))
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

        public WorldStatus WorldStatus { get; private set; }
        public int Radius { get { return radius; } }
        public int MaxWorldHeight { get { return chunkRowHeight * ChunkSize; } }
        public int BuildWorldProgress { get; private set; } // Used for loading bar

        private bool isInitialBuild = true; // Is this build the first one?

        protected override void Awake()
        {
            base.Awake();
            chunkDatabase = new Dictionary<string, Chunk>();
            ChunkSize = chunkSize;
        }

        public void StartInitialBuildWorld() // This is when the player enters to the game the first time
        {
            BuildWorldProgress = 0;
            StartCoroutine(BuildWorld());
        }

        private void Update()
        {
            if (WorldStatus != WorldStatus.Building && !isInitialBuild)
            {
                StartCoroutine(BuildWorld());
            }
        }

        private IEnumerator BuildWorld()
        {
            WorldStatus = WorldStatus.Building;

            int playerPositionX = Mathf.FloorToInt(playerTransform.position.x / ChunkSize);
            int playerPositionZ = Mathf.FloorToInt(playerTransform.position.z / ChunkSize);

            UpdateProgress(10);

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


                        string chunkID = GetChunkID(chunkPosition);
                        Chunk currentChunk = GetChunk(chunkID);
                        // Chunk already exists, keep it
                        if (currentChunk != null)
                        {
                            currentChunk.ChunkStatus = ChunkStatus.Keep;
                            break;
                        }
                        // Chunk doesn't exist, draw it
                        else
                        {
                            currentChunk = new Chunk(chunkPosition, worldTextureAtlas, transform)
                            {
                                ChunkStatus = ChunkStatus.Draw
                            };

                            chunkDatabase.Add(chunkID, currentChunk);
                        }

                        yield return null;
                    }
                }
            }

            UpdateProgress(30);

            // Build initialised chunks
            foreach (KeyValuePair<string, Chunk> chunk in chunkDatabase)
            {
                if (chunk.Value.ChunkStatus == ChunkStatus.Draw)
                {
                    chunk.Value.BuildChunk();
                }

                yield return null;
            }

            UpdateProgress(30);

            // Build chunk blocks
            foreach (KeyValuePair<string, Chunk> chunk in chunkDatabase)
            {
                if (chunk.Value.ChunkStatus == ChunkStatus.Draw)
                {
                    chunk.Value.BuildChunkBlocks();
                    chunk.Value.ChunkStatus = ChunkStatus.Keep;
                }

                // TODO delete old chunks

                chunk.Value.ChunkStatus = ChunkStatus.Done;

                yield return null;
            }

            CurrentBuildComplete();
        }

        private void CurrentBuildComplete()
        {
            SpawnPlayer();
            UpdateProgress(30);

            WorldStatus = WorldStatus.None;
            if (isInitialBuild)
            {
                isInitialBuild = false; // We've already built the initial (start) world
            }
        }

        private void SpawnPlayer()
        {
            if (isInitialBuild)
            {
                playerTransform = PlayerManager.Instance.SpawnPlayer(MaxWorldHeight);
            }
        }

        private void UpdateProgress(int progress)
        {
            if (isInitialBuild)
            {
                BuildWorldProgress += progress;
            }
        }
    }
}