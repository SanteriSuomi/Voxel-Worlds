using System.Collections;
using System.IO;
using System.Runtime.Serialization;
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
            var surrogates = new SurrogateSelector();
            surrogates.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), new Vector3Surrogate());
            surrogates.AddSurrogate(typeof(Quaternion), new StreamingContext(StreamingContextStates.All), new QuaternionSurrogate());
            bf = new BinaryFormatter
            {
                SurrogateSelector = surrogates
            };
        }

        public string BuildChunkFilePath(Vector3Int chunkPosition)
        {
            return $"{Application.persistentDataPath}/{saveFolderName}/ChunkData/Chunk_{chunkPosition}_{WorldManager.Instance.ChunkSize}_{WorldManager.Instance.Radius}.dat";
        }

        public string BuildFilePath(string fileName)
        {
            return $"{Application.persistentDataPath}/{saveFolderName}/{fileName}";
        }

        /// <summary>
        /// Save a chunk to it's own dedicated file.
        /// </summary>
        /// <param name="chunk"></param>
        public IEnumerator Save(Chunk chunk)
        {
            string chunkFile = BuildChunkFilePath(new Vector3Int((int)chunk.GameObject.transform.position.x,
                                                                 (int)chunk.GameObject.transform.position.y,
                                                                 (int)chunk.GameObject.transform.position.z));

            ValidateDirectory(chunkFile);
            ChunkData newChunkData = new ChunkData(chunk.GetBlockTypeData(), chunk.GameObject.transform.position);
            using (var fs = new FileStream(chunkFile, FileMode.Create))
            {
                bf.Serialize(fs, newChunkData);
            }

            Destroy(chunk.GameObject);
            yield break;
        }

        /// <summary>
        /// Save any object to a path of your choosing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">Object to save.</param>
        /// <param name="path">Path to save the object to.</param>
        public void Save<T>(T obj, string path)
        {
            ValidateDirectory(path);
            using (var fs = new FileStream(path, FileMode.Create))
            {
                bf.Serialize(fs, obj);
            }
        }

        /// <summary>
        /// Load a chunk.
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns>Tuple that includes whether or not the load was succesfull and the chunk's data if so.</returns>
        public (bool, ChunkData) Load(Chunk chunk)
        {
            string chunkFile = BuildChunkFilePath(new Vector3Int((int)chunk.GameObject.transform.position.x,
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

        /// <summary>
        /// Load an objet.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns>Bool that indicates load success and the objet itself.</returns>
        public (bool, T) Load<T>(string path)
        {
            if (File.Exists(path))
            {
                T data;
                using (var fs = new FileStream(path, FileMode.Open))
                {
                    data = (T)bf.Deserialize(fs);
                }

                return (true, data);
            }

            #if UNITY_EDITOR
            Debug.LogWarning($"File {path} does not exist! Ignore if first time playing.");
            #endif
            return default;
        }

        /// <summary>
        /// Is a particular chunk saved?
        /// </summary>
        /// <param name="chunk"></param>
        public bool Exists(Chunk chunk)
        {
            return File.Exists(BuildChunkFilePath(new Vector3Int((int)chunk.GameObject.transform.position.x,
                                                                 (int)chunk.GameObject.transform.position.y,
                                                                 (int)chunk.GameObject.transform.position.z)));
        }

        private static void ValidateDirectory(string file)
        {
            if (!Directory.Exists(Path.GetDirectoryName(file)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
            }
        }
    }
}