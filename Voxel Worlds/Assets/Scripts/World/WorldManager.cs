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
            WorldStatus = WorldStatus.Building;
            Vector3Int playerInitialPosition = new Vector3Int(0,
                                                           MaxWorldHeight / 2,
                                                           0);
            lastBuildPosition = playerInitialPosition;
            Vector3Int playerChunkPosition = playerInitialPosition / ChunkSize;

            UpdateProgressBarWith(10);
            InitializeChunkAt(playerChunkPosition.x, playerChunkPosition.y, playerChunkPosition.z);
            UpdateProgressBarWith(30);
            StartCoroutine(BuildWorldRecursive(playerChunkPosition.x,
                                               playerChunkPosition.y,
                                               playerChunkPosition.z, radius));
            UpdateProgressBarWith(30);
            StartCoroutine(BuildInitializedChunks());
            UpdateProgressBarWith(30);
            StartCoroutine(CompleteInitialization(playerInitialPosition));
        }

        private void UpdateProgressBarWith(int progress)
        {
            if (isInitialBuild)
            {
                BuildWorldProgress += progress;
            }
        }

        private IEnumerator CompleteInitialization(Vector3 playerInitialPosition)
        {
            yield return new WaitUntil(() => !isInitialBuild);
            SpawnPlayer();
            WorldStatus = WorldStatus.Idle;

            void SpawnPlayer()
            {
                EventManager.TriggerEvent("BuildWorldComplete");
                playerTransform = PlayerManager.Instance.SpawnPlayer(playerInitialPosition);
            }
        }

        private void Update()
        {
            if (isInitialBuild) return;
            float playerDistanceFromLastBuildPos = (lastBuildPosition - playerTransform.position).sqrMagnitude;
            if (playerDistanceFromLastBuildPos > ChunkSize * 2)
            {
                lastBuildPosition = playerTransform.position;
                Vector3Int playerChunkPosition = new Vector3Int((int)lastBuildPosition.x,
                                                                (int)lastBuildPosition.y,
                                                                (int)lastBuildPosition.z) / ChunkSize;

                BuildNearPlayer(playerChunkPosition.x, playerChunkPosition.y, playerChunkPosition.z);
            }
        }

        private void BuildNearPlayer(int x, int y, int z)
        {
            WorldStatus = WorldStatus.Building;
            StopCoroutine(nameof(BuildWorldRecursive));
            StartCoroutine(BuildWorldRecursive(x, y, z, radius));
            StartCoroutine(BuildInitializedChunks());
        }

        private IEnumerator BuildWorldRecursive(int x, int y, int z, int radius)
        {
            radius--;
            if (radius <= 0)
            {
                yield break;
            }

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
            WaitForSeconds timer = null;
            if (isInitialBuild)
            {
                timer = new WaitForSeconds(3.5f);
                yield return timer;
            }

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

            if (isInitialBuild)
            {
                yield return timer;
                isInitialBuild = false;
            }
            else if (WorldStatus == WorldStatus.Building)
            {
                WorldStatus = WorldStatus.Idle;
            }
        }
    }
}