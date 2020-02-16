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
        private ConcurrentDictionary<string, Chunk> chunkDatabase;

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
        private Vector3 lastBuildPosition; // Keep track of where chunks we build around player last time

        private bool isInitialBuild = true; // Keep track on when first time build is complete

        protected override void Awake()
        {
            base.Awake();
            chunkDatabase = new ConcurrentDictionary<string, Chunk>();
            ChunkSize = chunkSize;
        }

        public void InitializeWorld()
        {
            Vector3 playerInitialPosition = new Vector3(playerTransform.position.x,
                                                       (playerTransform.position.y + MaxWorldHeight) / 2,
                                                        playerTransform.position.z);
            lastBuildPosition = playerInitialPosition;
            Vector3 playerChunkPosition = new Vector3(playerInitialPosition.x / ChunkSize,
                                                      playerInitialPosition.y / ChunkSize,
                                                      playerInitialPosition.z / ChunkSize);
            // Chunk position relative to the player converted to integer
            int playerChunkPosX = (int)playerChunkPosition.x;
            int playerChunkPosY = (int)playerChunkPosition.y;
            int playerChunkPosZ = (int)playerChunkPosition.z;

            UpdateProgress(10); // Updating the progress bad
            InitializeChunkAt(playerChunkPosX, playerChunkPosY, playerChunkPosZ);
            UpdateProgress(30);
            StartCoroutine(BuildInitializedChunks());
            UpdateProgress(30);
            StartCoroutine(BuildWorldRecursive(playerChunkPosX, playerChunkPosY, playerChunkPosZ, radius));
            UpdateProgress(30);
            StartCoroutine(InitializationComplete());
        }

        #region Initialization
        private void UpdateProgress(int progress)
        {
            if (isInitialBuild)
            {
                BuildWorldProgress += progress;
            }
        }

        private IEnumerator InitializationComplete()
        {
            yield return new WaitForSeconds(5);
            SpawnPlayer();
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
                playerTransform = PlayerManager.Instance.SpawnPlayer(new Vector3(0, MaxWorldHeight, 0));
            }
        }
        #endregion

        private void Update()
        {
            Vector3 playerMovement = lastBuildPosition - playerTransform.position;
            if (playerMovement.sqrMagnitude > ChunkSize * 2)
            {
                lastBuildPosition = playerTransform.position;
                BuildNearPlayer();
            }
        }

        private void BuildNearPlayer()
        {
            StopCoroutine(nameof(BuildWorldRecursive));
            int chunkPositionX = (int)(playerTransform.position.x / ChunkSize);
            int chunkPositionY = (int)(playerTransform.position.y / ChunkSize);
            int chunkPositionZ = (int)(playerTransform.position.z / ChunkSize);
            StartCoroutine(BuildWorldRecursive(chunkPositionX, chunkPositionY, chunkPositionZ, radius));
            StartCoroutine(BuildInitializedChunks());
        }

        private IEnumerator BuildWorldRecursive(int x, int y, int z, int radius)
        {
            radius--;
            if (radius <= 0) yield break;

            // Front
            InitializeChunkAt(x, y, z + 1);
            StartCoroutine(BuildWorldRecursive(x, y, z + 1, radius));
            yield return null;

            // Back
            InitializeChunkAt(x, y, z - 1);
            StartCoroutine(BuildWorldRecursive(x, y, z - 1, radius));
            yield return null;

            // Right
            InitializeChunkAt(x + 1, y, z);
            StartCoroutine(BuildWorldRecursive(x + 1, y, z, radius));
            yield return null;

            // Left
            InitializeChunkAt(x - 1, y, z);
            StartCoroutine(BuildWorldRecursive(x - 1, y, z, radius));
            yield return null;

            // Top
            InitializeChunkAt(x, y + 1, z);
            StartCoroutine(BuildWorldRecursive(x, y + 1, z, radius));
            yield return null;

            // Bottom
            InitializeChunkAt(x, y - 1, z);
            StartCoroutine(BuildWorldRecursive(x, y - 1, z, radius));
            yield return null;
        }

        private void InitializeChunkAt(int x, int y, int z)
        {
            Vector3 chunkPosition = new Vector3(x * ChunkSize,
                                                y * ChunkSize,
                                                z * ChunkSize);

            string chunkID = GetChunkID(chunkPosition);
            Chunk currentChunk = GetChunk(chunkID);
            if (currentChunk == null)
            {
                currentChunk = new Chunk(chunkPosition, worldTextureAtlas, transform)
                {
                    ChunkStatus = ChunkStatus.Draw // Signal that this chunk can be drawn
                };

                chunkDatabase.TryAdd(chunkID, currentChunk);
            }
        }

        private IEnumerator BuildInitializedChunks()
        {
            // Build chunks
            foreach (KeyValuePair<string, Chunk> chunk in chunkDatabase)
            {
                if (chunk.Value.ChunkStatus == ChunkStatus.Draw)
                {
                    chunk.Value.BuildChunk();
                }

                yield return null;
            }

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
        }

        //public void StartInitialBuildWorld() // This is when the player enters to the game the first time
        //{
        //    BuildWorldProgress = 0;
        //    StartCoroutine(BuildWorld());
        //}

        //private void Update()
        //{
        //    if (WorldStatus != WorldStatus.Building && !isInitialBuild)
        //    {
        //        StartCoroutine(BuildWorld());
        //    }
        //}

        //private IEnumerator BuildWorld()
        //{
        //    WorldStatus = WorldStatus.Building;

        //    int playerPositionX = Mathf.FloorToInt(playerTransform.position.x / ChunkSize);
        //    int playerPositionZ = Mathf.FloorToInt(playerTransform.position.z / ChunkSize);

        //    UpdateProgress(10);

        //    // Initialise chunks around player
        //    for (int x = -Radius; x <= Radius; x++)
        //    {
        //        for (int z = -Radius; z <= Radius; z++)
        //        {
        //            for (int y = 0; y < chunkRowHeight; y++)
        //            {
        //                Vector3 chunkPosition = new Vector3((x + playerPositionX) * ChunkSize,
        //                                                     y * ChunkSize,
        //                                                    (z + playerPositionZ) * ChunkSize);


        //                string chunkID = GetChunkID(chunkPosition);
        //                Chunk currentChunk = GetChunk(chunkID);
        //                // Chunk already exists, keep it
        //                if (currentChunk != null)
        //                {
        //                    currentChunk.ChunkStatus = ChunkStatus.Keep;
        //                    break;
        //                }
        //                // Chunk doesn't exist, draw it
        //                else
        //                {
        //                    currentChunk = new Chunk(chunkPosition, worldTextureAtlas, transform)
        //                    {
        //                        ChunkStatus = ChunkStatus.Draw
        //                    };

        //                    chunkDatabase.TryAdd(chunkID, currentChunk);
        //                }

        //                yield return null;
        //            }
        //        }
        //    }

        //    UpdateProgress(30);

        //    // Build initialised chunks
        //    foreach (KeyValuePair<string, Chunk> chunk in chunkDatabase)
        //    {
        //        if (chunk.Value.ChunkStatus == ChunkStatus.Draw)
        //        {
        //            chunk.Value.BuildChunk();
        //        }

        //        yield return null;
        //    }

        //    UpdateProgress(30);

        //    // Build chunk blocks
        //    foreach (KeyValuePair<string, Chunk> chunk in chunkDatabase)
        //    {
        //        if (chunk.Value.ChunkStatus == ChunkStatus.Draw)
        //        {
        //            chunk.Value.BuildChunkBlocks();
        //            chunk.Value.ChunkStatus = ChunkStatus.Keep;
        //        }

        //        // TODO delete old chunks

        //        chunk.Value.ChunkStatus = ChunkStatus.Done;

        //        yield return null;
        //    }

        //    CurrentBuildComplete();
        //}
    }
}