using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Voxel.Utility;
using Voxel.World;

namespace Voxel.Saving
{
    [Serializable]
    public class ChunkData
    {
        public BlockType[,,] BlockTypeData { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public ChunkData() { }

        public ChunkData(BlockType[,,] blockTypeData, Vector3 chunkPosition)
        {
            X = chunkPosition.x;
            Y = chunkPosition.y;
            Z = chunkPosition.z;

            BlockTypeData = blockTypeData;
        }
    }

    public class ChunkSaveManager : Singleton<ChunkSaveManager>
    {
        private BinaryFormatter bf;

        [SerializeField]
        private string saveFolderName = "SaveData";

        protected override void Awake()
        {
            base.Awake();
            bf = new BinaryFormatter();
        }

        public string BuildChunkFileName(Vector3Int chunkPosition)
        {
            return $"{Application.persistentDataPath}/{saveFolderName}/Chunk_{chunkPosition}_{WorldManager.Instance.ChunkSize}.dat";
        }

        public IEnumerator Save(Chunk chunk)
        {
            string chunkFile = BuildChunkFileName(new Vector3Int((int)chunk.GameObject.transform.position.x,
                                                                 (int)chunk.GameObject.transform.position.y,
                                                                 (int)chunk.GameObject.transform.position.z));
            if (!File.Exists(chunkFile))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(chunkFile));
            }

            ChunkData newChunkData = new ChunkData(chunk.GetBlockTypeData(), chunk.GameObject.transform.position);
            using (var fs = new FileStream(chunkFile, FileMode.Create))
            {
                bf.Serialize(fs, newChunkData);
            }

            yield break;
        }

        public (bool, ChunkData) Load(Chunk chunk)
        {
            string chunkFile = BuildChunkFileName(new Vector3Int((int)chunk.GameObject.transform.position.x,
                                                                 (int)chunk.GameObject.transform.position.y,
                                                                 (int)chunk.GameObject.transform.position.z));
            if (File.Exists(chunkFile))
            {
                ChunkData chunkData;
                using (var fs = new FileStream(chunkFile, FileMode.Open))
                {
                    chunkData = (ChunkData)bf.Deserialize(fs);
                }

                return (true, chunkData);
            }

            return (false, null);
        }

        public bool Exists(Chunk chunk)
        {
            string chunkFile = BuildChunkFileName(new Vector3Int((int)chunk.GameObject.transform.position.x,
                                                                 (int)chunk.GameObject.transform.position.y,
                                                                 (int)chunk.GameObject.transform.position.z));
            return File.Exists(chunkFile);
        }
    }
}