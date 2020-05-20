using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Voxel.Utility;
using Voxel.World;

namespace Voxel.Saving
{
    public class SaveManager : Singleton<SaveManager>
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
            return $"{Application.persistentDataPath}/{saveFolderName}/Chunk_{chunkPosition}_{WorldManager.Instance.ChunkSize}_{WorldManager.Instance.Radius}.dat";
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

            Destroy(chunk.GameObject);
            yield break;
        }

        public void Save<T>(T obj)
        {

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

        public (bool, T) Load<T>()
        {
            return default;
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