using System;
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

        public ChunkData() { }

        public ChunkData(Block[,,] blockData)
        {
            int chunkSize = WorldManager.Instance.ChunkSize;
            BlockTypeData = new BlockType[chunkSize, chunkSize, chunkSize];
            for (int x = 0; x < blockData.GetUpperBound(0); x++)
            {
                for (int y = 0; y < blockData.GetUpperBound(1); y++)
                {
                    for (int z = 0; z < blockData.GetUpperBound(2); z++)
                    {
                        BlockTypeData[x, y, z] = blockData[x, y, z].BlockType;
                    }
                }
            }
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

        public string BuildChunkFileName(Vector3 chunkPosition)
        {
            return $"{Application.persistentDataPath}/{saveFolderName}/Chunk_{chunkPosition}_{WorldManager.Instance.ChunkSize}.dat";
        }

        public (bool, ChunkData) Load(Chunk chunk)
        {
            string chunkFile = BuildChunkFileName(chunk.ChunkGameObject.transform.position);
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

        public bool Save(Chunk chunk, ChunkData chunkData)
        {
            if (chunk == null || chunkData == null)
            {
                return false;
            }

            string chunkFile = BuildChunkFileName(chunk.ChunkGameObject.transform.position);
            if (!File.Exists(chunkFile))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(chunkFile));
            }
            
            using (var fs = new FileStream(chunkFile, FileMode.Create))
            {
                bf.Serialize(fs, chunkData);
            }

            return true;
        }

        public void BuildChunk(Chunk chunk)
        {

        }
    }
}