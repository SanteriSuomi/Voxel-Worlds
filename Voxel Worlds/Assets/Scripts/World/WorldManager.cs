using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using Voxel.Game;
using Voxel.Player;
using Voxel.Utility;

namespace Voxel.World
{
    public enum WorldStatus
    {
        Idle,
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

        private CoroutineQueue coroutineQueue;
        public uint MaxQueueCoroutines { get; private set; }
        [SerializeField]
        private uint maxQueueCoroutinesAllowed = 1000;

        [Header("Chunk and World Settings")]
        [SerializeField]
        private int chunkRowHeight = 8;
        [SerializeField]
        private int chunkSize = 16;
        public int ChunkSize { get; private set; }
        [SerializeField]
        private int buildRadius = 4;

        public WorldStatus WorldStatus { get; private set; }
        public int Radius { get { return buildRadius; } }
        public int MaxWorldHeight { get { return chunkRowHeight * ChunkSize; } }
        public int BuildWorldProgress { get; private set; } // Used for loading bar

        private Vector3 lastBuildPosition; // Keep track of where chunks we build around player last time

        protected override void Awake()
        {
            base.Awake();
            chunkDatabase = new ConcurrentDictionary<string, Chunk>();
            MaxQueueCoroutines = maxQueueCoroutinesAllowed;
            coroutineQueue = new CoroutineQueue(MaxQueueCoroutines, StartCoroutine);
            ChunkSize = chunkSize;
        }

        public void InitializeWorld()
        {
            WorldStatus = WorldStatus.Building;
            StartCoroutine(InitializeWorldCoroutine());
        }

        private IEnumerator InitializeWorldCoroutine()
        {
            Vector3Int playerInitialPosition = new Vector3Int(0,
                                                              MaxWorldHeight / 2,
                                                              0);
            lastBuildPosition = playerInitialPosition;
            Vector3Int playerChunkPosition = playerInitialPosition / ChunkSize;

            UpdateProgressBarWith(10);
            InitializeChunkAt(playerChunkPosition.x, playerChunkPosition.y, playerChunkPosition.z);
            UpdateProgressBarWith(30);
            yield return StartCoroutine(BuildWorldRecursive(playerChunkPosition.x,
                                                                playerChunkPosition.y,
                                                                playerChunkPosition.z,
                                                                buildRadius * 4));
            UpdateProgressBarWith(30);
            yield return StartCoroutine(BuildInitializedChunks());
            UpdateProgressBarWith(30);
            CompleteInitialization(playerInitialPosition);
            StartCoroutine(UpdateLoop());
        }

        private void UpdateProgressBarWith(int progress) => BuildWorldProgress += progress;

        private void CompleteInitialization(Vector3 playerInitialPosition)
        {
            WorldStatus = WorldStatus.Idle;
            EventManager.TriggerEvent("BuildWorldComplete");
            playerTransform = PlayerManager.Instance.SpawnPlayer(playerInitialPosition);
        }

        private IEnumerator UpdateLoop()
        {
            while (GameManager.Instance.IsGameRunning)
            {
                float distanceFromLastBuildPosition = (lastBuildPosition - playerTransform.position).magnitude;
                if (distanceFromLastBuildPosition > ChunkSize)
                {
                    lastBuildPosition = playerTransform.position;
                    Vector3Int playerChunkPosition = new Vector3Int((int)lastBuildPosition.x,
                                                                    (int)lastBuildPosition.y,
                                                                    (int)lastBuildPosition.z) / ChunkSize;

                    StartCoroutine(BuildNearPlayer(playerChunkPosition.x, playerChunkPosition.y, playerChunkPosition.z));
                }

                yield return null;
            }
        }

        private IEnumerator BuildNearPlayer(int x, int y, int z)
        {
            WorldStatus = WorldStatus.Building;
            //StopCoroutine(nameof(BuildWorldRecursive));
            yield return StartCoroutine(BuildWorldRecursive(x, y, z, buildRadius));
            yield return StartCoroutine(BuildInitializedChunks());
            WorldStatus = WorldStatus.Idle;
        }

        private IEnumerator BuildWorldRecursive(int x, int y, int z, int radius)
        {
            //Debug.Log("buildworldrecursive");
            radius--;
            if (radius <= 0)
            {
                yield break;
            }

            // Front
            InitializeChunkAt(x, y, z + 1);
            coroutineQueue.Run(BuildWorldRecursive(x, y, z + 1, radius));
            yield return null;

            // Back
            InitializeChunkAt(x, y, z - 1);
            coroutineQueue.Run(BuildWorldRecursive(x, y, z - 1, radius));
            yield return null;

            // Right
            InitializeChunkAt(x + 1, y, z);
            coroutineQueue.Run(BuildWorldRecursive(x + 1, y, z, radius));
            yield return null;

            // Left
            InitializeChunkAt(x - 1, y, z);
            coroutineQueue.Run(BuildWorldRecursive(x - 1, y, z, radius));
            yield return null;

            // Top
            InitializeChunkAt(x, y + 1, z);
            coroutineQueue.Run(BuildWorldRecursive(x, y + 1, z, radius));
            yield return null;

            // Bottom
            InitializeChunkAt(x, y - 1, z);
            coroutineQueue.Run(BuildWorldRecursive(x, y - 1, z, radius));
            yield return null;
        }

        private void InitializeChunkAt(int x, int y, int z)
        {
            Vector3Int chunkPosition = new Vector3Int(x * (ChunkSize - 1), // -1 from chunkSize because otherwise there would be 1 block gap between chunks. 
                                                      y * (ChunkSize - 1), // Cause unknown at this time.
                                                      z * (ChunkSize - 1));
            if (chunkPosition.y < 0) return; // Don't create chunks below bedrock

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
            //Debug.Log("buildinitializedchunks");
            // Build chunks
            foreach (KeyValuePair<string, Chunk> chunk in chunkDatabase)
            {
                if (chunk.Value.ChunkStatus == ChunkStatus.Draw)
                {
                    chunk.Value.BuildChunk();
                }

                yield return null;
            }

            foreach (KeyValuePair<string, Chunk> chunk in chunkDatabase)
            {
                if (chunk.Value.ChunkStatus == ChunkStatus.Draw)
                {
                    chunk.Value.BuildBlocks();
                    chunk.Value.ChunkStatus = ChunkStatus.Keep;
                }

                // TODO hide old chunks

                chunk.Value.ChunkStatus = ChunkStatus.Done;
                yield return null;
            }

            WorldStatus = WorldStatus.Building;
        }
    }
}