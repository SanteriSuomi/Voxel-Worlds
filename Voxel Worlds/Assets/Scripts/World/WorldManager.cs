﻿using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxel.Game;
using Voxel.Other;
using Voxel.Player;
using Voxel.Saving;
using Voxel.Utility;

namespace Voxel.World
{
    public enum WorldStatus
    {
        Idle,
        Processing
    }

    public class WorldManager : Singleton<WorldManager>
    {
        private ConcurrentDictionary<string, Chunk> chunkDatabase;
        private readonly Stack<string> chunksToRemove = new Stack<string>();

        /// <summary>
        /// All currently active hit decals in the world.
        /// </summary>
        public ConcurrentDictionary<string, HitDecal> HitDecalDatabase { get; private set; }

        #region Sand Block Database
        /// <summary>
        /// Contains a database of if a sand block has already been placed at a location, used when generating water.
        /// </summary>
        private ConcurrentDictionary<string, bool> sandBlockDatabase;

        public void AddSandBlock(Transform chunk, Vector3Int pos)
            => sandBlockDatabase.TryAdd($"{chunk.position.x + pos.x}_{chunk.position.z + pos.z}", true);

        public bool ContainsSandBlock(Transform chunk, Vector3Int pos)
            => sandBlockDatabase.TryGetValue($"{chunk.position.x + pos.x}_{chunk.position.z + pos.z}", out _);
        #endregion

        #region Get Chunk Methods
        /// <summary>
        /// Return the ID (as a string) of a chunk at position as specified in the ChunkDictionary.
        /// </summary>
        /// <param name="fromPosition">World position of the chunk parent.</param>
        public string GetChunkID(Vector3 fromPosition)
        {
            return $"{(int)fromPosition.x} {(int)fromPosition.y} {(int)fromPosition.z}";
        }

        /// <summary>
        /// Get the chunk by it's ID (string) normally used with the GetChunkID method.
        /// </summary>
        /// <param name="chunkID">Chunk ID (position as string)</param>
        public Chunk GetChunkFromID(string chunkID)
        {
            if (chunkDatabase.TryGetValue(chunkID, out Chunk chunk))
            {
                return chunk;
            }

            return null;
        }

        /// <summary>
        /// Combine GetChunkID and GetChunkFromID to get a chunk from the database.
        /// </summary>
        /// <param name="fromPosition"></param>
        public Chunk GetChunk(Vector3 fromPosition)
        {
            if (chunkDatabase.TryGetValue(GetChunkID(fromPosition), out Chunk chunk))
            {
                return chunk;
            }

            return null;
        }

        /// <summary>
        /// Approximately get a chunk from a world position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns>The chunk.</returns>
        public Chunk GetChunkFromWorldPosition(Vector3 position)
        {
            float chunkMidSize = (float)chunkEdgeSize / 2;

            // Get the (approx) middle point of a world chunk.
            Vector3 middlePoint = new Vector3(MathUtils.GetNearestMultipleOf(position.x, chunkMidSize) - 2,
                                              MathUtils.GetNearestMultipleOf(position.y, chunkMidSize),
                                              MathUtils.GetNearestMultipleOf(position.z, chunkMidSize) - 2);

            // Convert that middle point to the chunk position.
            Vector3 playerChunkPosition = new Vector3(Mathf.FloorToInt(MathUtils.GetNearestMultipleOf(middlePoint.x, chunkEdgeSize)),
                                                      Mathf.FloorToInt(MathUtils.GetNearestMultipleOf(middlePoint.y, chunkEdgeSize)),
                                                      Mathf.FloorToInt(MathUtils.GetNearestMultipleOf(middlePoint.z, chunkEdgeSize)));

            return GetChunk(playerChunkPosition);
        }

        public ConcurrentDictionary<string, Chunk> GetAllChunks() => chunkDatabase;
        #endregion

        [Header("Misc. Dependencies")]
        [SerializeField]
        private Transform playerTransform = default;

        [Header("Chunk and World Settings")]
        [SerializeField]
        private int chunkRowHeight = 8;
        [SerializeField]
        private int chunkSize = 16;
        public int ChunkSize { get; private set; }
        private int chunkEdgeSize; // ChunkSize except taking array indexes into account
        [SerializeField]
        private int buildRadius = 4;
        [SerializeField]
        private int initialBuildRadius = 8;
        [SerializeField]
        private float buildNearPlayerDistanceMultiplier = 0.5f;
        [SerializeField]
        private float removeOldChunksDistanceMultiplier = 2;
        [SerializeField]
        private int amountOfBuildsUntilRemoving = 3;

        [Header("World Generation Misc. Settings")]
        [SerializeField]
        private float buildForwardMultiplier = 5;
        [SerializeField]
        private float autosaveInterval = 60;
        private float autoSaveTimer;
        [SerializeField, Tooltip("Number of frames to skip until waiting a frame. Used by all the chunk update functions concecutively.")]
        private int maxSkippedFramesUntilWait = 3;
        private int skipFrameCounter;

        public WorldStatus WorldStatus { get; private set; }
        public int Radius { get { return buildRadius; } }
        public int MaxWorldHeight { get { return chunkRowHeight * ChunkSize; } }
        public int BuildWorldProgress { get; private set; } // Used for loading bar
        public int ChunkEdge => ChunkSize - 2;

        private int chunkStatusDoneAmount; // Amount of chunks with the "completed" status, used by loading bar.
        private int removeOldChunksIndex;
        private Vector3 lastBuildPosition; // Keep track of where chunks we build around player last time

        protected override void Awake()
        {
            base.Awake();
            chunkDatabase = new ConcurrentDictionary<string, Chunk>();
            HitDecalDatabase = new ConcurrentDictionary<string, HitDecal>();
            sandBlockDatabase = new ConcurrentDictionary<string, bool>();
            ChunkSize = chunkSize;
            chunkEdgeSize = ChunkSize - 1;
        }

        public void InitializeWorld()
        {
            WorldStatus = WorldStatus.Processing;
            PlayerManager.Instance.Load();
            StartCoroutine(InitializeWorldCoroutine());
        }

        private IEnumerator InitializeWorldCoroutine()
        {
            Vector3Int playerChunkPosition = CalculateInitialPositions();
            StartCoroutine(UpdateWorldBuildProgress());
            yield return StartCoroutine(InitializeChunksInRadius(playerChunkPosition.x,
                                                                 playerChunkPosition.y,
                                                                 playerChunkPosition.z,
                                                                 initialBuildRadius));
            yield return StartCoroutine(BuildInitializedChunks());
            EndWorldInitialize();
            StartCoroutine(UpdateLoop()); // Start the main update loop
        }

        private Vector3Int CalculateInitialPositions()
        {
            Vector3 initialPosition = PlayerManager.Instance.InitialPosition;
            lastBuildPosition = initialPosition;
            return new Vector3Int((int)initialPosition.x,
                                  (int)initialPosition.y,
                                  (int)initialPosition.z) / ChunkSize;
        }

        private IEnumerator UpdateWorldBuildProgress()
        {
            while (BuildWorldProgress < 100)
            {
                BuildWorldProgress = chunkStatusDoneAmount / initialBuildRadius;
                yield return null;
            }
        }

        private void EndWorldInitialize()
        {
            WorldStatus = WorldStatus.Idle;
            EventManager.TriggerEvent("BuildWorldComplete");
            playerTransform = PlayerManager.Instance.SpawnPlayer();
        }

        private IEnumerator UpdateLoop()
        {
            while (GameManager.Instance.IsGameRunning)
            {
                float distanceFromLastBuildPosition = (lastBuildPosition - playerTransform.position).magnitude;
                TryAutosave(distanceFromLastBuildPosition);
                if (distanceFromLastBuildPosition > ChunkSize * buildNearPlayerDistanceMultiplier)
                {

                    Vector3 playerVelocityNormalized = (PlayerManager.Instance.CharacterController.velocity * 10).normalized;

                    lastBuildPosition = playerTransform.position;
                    Vector3 directionMultiplier = playerVelocityNormalized * ChunkSize * buildForwardMultiplier;
                    Vector3Int playerChunkPosition = new Vector3Int((int)(lastBuildPosition.x + directionMultiplier.x),
                                                                    (int)lastBuildPosition.y,
                                                                    (int)(lastBuildPosition.z + directionMultiplier.z)) / ChunkSize;

                    yield return StartCoroutine(ProcessChunksNearPlayer(playerChunkPosition.x,
                                                                        playerChunkPosition.y,
                                                                        playerChunkPosition.z));
                }

                yield return null;
            }
        }

        private void TryAutosave(float distanceFromLastBuildPosition)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autosaveInterval
                || distanceFromLastBuildPosition > ChunkSize * buildNearPlayerDistanceMultiplier * 2)
            {
                autoSaveTimer = 0;
                SaveManager.Instance.SaveAll();
            }
        }

        private IEnumerator ProcessChunksNearPlayer(int x, int y, int z)
        {
            WorldStatus = WorldStatus.Processing;
            yield return StartCoroutine(InitializeChunksInRadius(x, y, z, buildRadius));
            yield return StartCoroutine(BuildInitializedChunks());
            removeOldChunksIndex++;
            if (removeOldChunksIndex >= amountOfBuildsUntilRemoving)
            {
                removeOldChunksIndex = 0;
                StartCoroutine(RemoveOldChunks());
            }

            WorldStatus = WorldStatus.Idle;
        }

        private IEnumerator InitializeChunksInRadius(int x, int y, int z, int radius)
        {
            for (int xL = -radius; xL < radius; xL++)
            {
                for (int yL = -radius; yL < radius; yL++)
                {
                    for (int zL = -radius; zL < radius; zL++)
                    {
                        InitializeChunkAt(x + xL, y + yL, z + zL);
                        object obj = TryGetWaitFrame();
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
            Vector3Int chunkPosition = new Vector3Int(x * chunkEdgeSize,
                                                      y * chunkEdgeSize,
                                                      z * chunkEdgeSize);

            if (chunkPosition.y < 0) return; // Don't create chunks below a certain threshold (bedrock)

            string chunkID = GetChunkID(chunkPosition);
            Chunk currentChunk = GetChunkFromID(chunkID);
            if (currentChunk == null)
            {
                currentChunk = new Chunk(chunkPosition, transform)
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
            // Build chunks
            for (int i = 0; i < chunkDatabase.Count; i++)
            {
                Chunk chunk = chunkDatabase.ElementAt(i).Value;
                if (chunk.ChunkStatus == ChunkStatus.Draw)
                {
                    chunk.BuildChunk();
                    object obj = TryGetWaitFrame();
                    if (obj is null)
                    {
                        yield return obj;
                    }
                }
            }

            // Build trees
            for (int i = 0; i < chunkDatabase.Count; i++)
            {
                Chunk chunk = chunkDatabase.ElementAt(i).Value;
                chunk.TryStartTreeGeneration();
                object obj = TryGetWaitFrame();
                if (obj is null)
                {
                    yield return obj;
                }
            }

            // Build blocks
            for (int i = 0; i < chunkDatabase.Count; i++)
            {
                Chunk chunk = chunkDatabase.ElementAt(i).Value;
                if (chunk.ChunkStatus == ChunkStatus.Draw)
                {
                    chunk.BuildBlocks();
                    object obj = TryGetWaitFrame();
                    if (obj is null)
                    {
                        yield return obj;
                    }
                }

                chunk.ChunkStatus = ChunkStatus.Done;
                chunkStatusDoneAmount++;

                if (PlayerManager.Instance.ActivePlayer != null
                    && chunk.BlockGameObject != null)
                {
                    float distanceToChunk = (PlayerManager.Instance.ActivePlayer.position - chunk.BlockGameObject.transform.position).magnitude;
                    if (distanceToChunk > ChunkSize * removeOldChunksDistanceMultiplier)
                    {
                        chunksToRemove.Push(chunkDatabase.ElementAt(i).Key);
                    }
                }
            }
        }

        private IEnumerator RemoveOldChunks()
        {
            // Remove old chunks
            for (int i = 0; i < chunksToRemove.Count; i++)
            {
                string chunkToRemoveID = chunksToRemove.Pop();
                if (chunkDatabase.TryGetValue(chunkToRemoveID, out Chunk chunk))
                {
                    if (chunk.Enemies.Count > 0)
                    {
                        SaveManager.Instance.Save(chunk, true);
                    }

                    chunkDatabase.TryRemove(chunkToRemoveID, out _);
                    Destroy(chunk.BlockGameObject);
                    object obj = TryGetWaitFrame();
                    if (obj is null)
                    {
                        yield return obj;
                    }
                }
            }
        }

        private object TryGetWaitFrame()
        {
            skipFrameCounter++;
            if (skipFrameCounter >= maxSkippedFramesUntilWait)
            {
                skipFrameCounter = 0;
                return null;
            }

            return 0;
        }
    }
}