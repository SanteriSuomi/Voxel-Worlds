using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using Voxel.Game;
using Voxel.Player;
using Voxel.Saving;
using Voxel.Utility;

namespace Voxel.World
{
    public enum WorldStatus
    {
        Idle,
        Modifying
    }

    public class WorldManager : Singleton<WorldManager>
    {
        private ConcurrentDictionary<string, Chunk> chunkDatabase;
        private readonly Stack<string> chunksToRemove = new Stack<string>();

        /// <summary>
        /// Return the ID (as a string) of a chunk at position as specified in the ChunkDictionary.
        /// </summary>
        /// <param name="fromPosition"></param>
        public string GetChunkID(Vector3 fromPosition)
        {
            return $"{(int)fromPosition.x} {(int)fromPosition.y} {(int)fromPosition.z}";
        }

        /// <summary>
        /// Get the chunk by it's ID (string) normally used with the GetChunkID method.
        /// </summary>
        /// <param name="chunkID"></param>
        public Chunk GetChunkByID(string chunkID)
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
        private int buildRadius = 4;
        [SerializeField]
        private int initialBuildRadius = 8;
        [SerializeField]
        private float buildNearPlayerDistanceThresholdMultiplier = 0.5f;
        [SerializeField]
        private float removeOldChunksDistanceMultiplier = 2;

        [Header("World Generation Misc. Settings")]
        [SerializeField]
        private int loopsUntilWaitingAFrame = 5;

        public WorldStatus WorldStatus { get; private set; }
        public int Radius { get { return buildRadius; } }
        public int MaxWorldHeight { get { return chunkRowHeight * ChunkSize; } }
        public int BuildWorldProgress { get; private set; } // Used for loading bar
        private int chunkStatusDoneAmount; // Amount of chunks with the "completed" status, used by loading bar.

        private Vector3 lastBuildPosition; // Keep track of where chunks we build around player last time

        protected override void Awake()
        {
            base.Awake();
            chunkDatabase = new ConcurrentDictionary<string, Chunk>();
            ChunkSize = chunkSize;
        }

        public void InitializeWorld()
        {
            WorldStatus = WorldStatus.Modifying;
            StartCoroutine(InitializeWorldCoroutine());
        }

        private IEnumerator InitializeWorldCoroutine()
        {
            Vector3Int playerInitialPosition = new Vector3Int(0,
                                                              MaxWorldHeight / 2,
                                                              0);
            lastBuildPosition = playerInitialPosition;
            Vector3Int playerChunkPosition = playerInitialPosition / ChunkSize;

            StartCoroutine(UpdateProgressBar());
            yield return StartCoroutine(InitializeChunksInRadius(playerChunkPosition.x,
                                                                 playerChunkPosition.y,
                                                                 playerChunkPosition.z,
                                                                 initialBuildRadius));
            yield return StartCoroutine(BuildInitializedChunks());
            CompleteInitialization(playerInitialPosition);
            StartCoroutine(UpdateLoop());
        }

        private IEnumerator UpdateProgressBar()
        {
            while (BuildWorldProgress < 100)
            {
                BuildWorldProgress = chunkStatusDoneAmount / initialBuildRadius;
                yield return null;
            }
        }

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
                if (distanceFromLastBuildPosition > ChunkSize * buildNearPlayerDistanceThresholdMultiplier)
                {
                    lastBuildPosition = playerTransform.position;
                    Vector3Int playerChunkPosition = new Vector3Int((int)lastBuildPosition.x,
                                                                    (int)lastBuildPosition.y,
                                                                    (int)lastBuildPosition.z) / ChunkSize;

                    yield return StartCoroutine(ProcessChunksNearPlayer(playerChunkPosition.x,
                                                                        playerChunkPosition.y,
                                                                        playerChunkPosition.z));
                }

                yield return null;
            }
        }

        private IEnumerator ProcessChunksNearPlayer(int x, int y, int z)
        {
            WorldStatus = WorldStatus.Modifying;
            yield return StartCoroutine(InitializeChunksInRadius(x, y, z, buildRadius));
            yield return StartCoroutine(BuildInitializedChunks());
            yield return StartCoroutine(RemoveOldChunks());
            WorldStatus = WorldStatus.Idle;
        }

        private IEnumerator InitializeChunksInRadius(int x, int y, int z, int radius)
        {
            int waitFrameCounter = 0;
            for (int xL = -radius; xL < radius; xL++)
            {
                for (int yL = -radius; yL < radius; yL++)
                {
                    for (int zL = -radius; zL < radius; zL++)
                    {
                        InitializeChunkAt(x + xL, y + yL, z + zL);
                        object obj = CheckForFrameWait(ref waitFrameCounter);
                        if (obj is null)
                        {
                            yield return obj;
                        }
                    }
                }
            }
        }

        private void InitializeChunkAt(int x, int y, int z)
        {
            Vector3Int chunkPosition = new Vector3Int(x * (ChunkSize - 1), // -1 from chunkSize because otherwise there would be 1 block gap between chunks. 
                                                      y * (ChunkSize - 1), // Cause unknown at this time.
                                                      z * (ChunkSize - 1));

            if (chunkPosition.y < 0) return; // Don't create chunks below bedrock

            string chunkID = GetChunkID(chunkPosition);
            Chunk currentChunk = GetChunkByID(chunkID);
            if (currentChunk == null)
            {
                currentChunk = new Chunk(chunkPosition, worldTextureAtlas, transform, false)
                {
                    ChunkStatus = ChunkStatus.Draw // Signal that this chunk can be drawn
                };

                chunkDatabase.TryAdd(chunkID, currentChunk);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Bug", "S2259:Null pointers should not be dereferenced",
         Justification = "Method is a coroutine, and yielding a null value is meant.")]
        private IEnumerator BuildInitializedChunks()
        {
            int waitFrameCounter = 0;

            // Build chunks
            foreach (KeyValuePair<string, Chunk> chunk in chunkDatabase)
            {
                if (chunk.Value.ChunkStatus == ChunkStatus.Draw)
                {
                    chunk.Value.BuildChunk();
                    object obj = CheckForFrameWait(ref waitFrameCounter);
                    if (obj is null)
                    {
                        yield return obj;
                    }
                }
            }

            // Build blocks
            foreach (KeyValuePair<string, Chunk> chunk in chunkDatabase)
            {
                if (chunk.Value.ChunkStatus == ChunkStatus.Draw)
                {
                    chunk.Value.BuildBlocks();
                    object obj = CheckForFrameWait(ref waitFrameCounter);
                    if (obj is null)
                    {
                        yield return obj;
                    }
                }

                chunk.Value.ChunkStatus = ChunkStatus.Done;
                chunkStatusDoneAmount++;

                if (PlayerManager.Instance.ActivePlayer != null
                    && chunk.Value.GameObject != null)
                {
                    float distanceToChunk = (PlayerManager.Instance.ActivePlayer.position - chunk.Value.GameObject.transform.position).magnitude;
                    if (distanceToChunk > Radius * ChunkSize * removeOldChunksDistanceMultiplier)
                    {
                        chunksToRemove.Push(chunk.Key);
                    }
                }
            }
        }

        private IEnumerator RemoveOldChunks()
        {
            int waitFrameCounter = 0;

            // Remove old chunks
            for (int i = 0; i < chunksToRemove.Count; i++)
            {
                string chunkToRemoveID = chunksToRemove.Pop();
                if (chunkDatabase.TryGetValue(chunkToRemoveID, out Chunk chunk))
                {
                    yield return StartCoroutine(ChunkSaveManager.Instance.Save(chunk));
                    Destroy(chunk.GameObject);
                    chunkDatabase.TryRemove(chunkToRemoveID, out _);
                    object obj = CheckForFrameWait(ref waitFrameCounter);
                    if (obj is null)
                    {
                        yield return obj;
                    }
                }
            }
        }

        private object CheckForFrameWait(ref int value)
        {
            value++;
            if (value >= loopsUntilWaitingAFrame)
            {
                value = 0;
                return null;
            }

            return 0;
        }
    }
}