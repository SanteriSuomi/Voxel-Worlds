using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Voxel.Characters.Enemy;
using Voxel.Characters.Saving;
using Voxel.Items.Inventory;
using Voxel.Player;
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
            InitializeBinaryFormatter();
        }

        private void InitializeBinaryFormatter()
        {
            var surrogates = new SurrogateSelector();
            surrogates.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), new Vector3Surrogate());
            surrogates.AddSurrogate(typeof(Quaternion), new StreamingContext(StreamingContextStates.All), new QuaternionSurrogate());
            bf = new BinaryFormatter
            {
                SurrogateSelector = surrogates
            };
        }

        public string BuildChunkFilePath(Vector3Int chunkPosition)
            => $"{Application.persistentDataPath}/{saveFolderName}/ChunkData/Chunk_{chunkPosition}_{WorldManager.Instance.ChunkSize}_{WorldManager.Instance.Radius}.dat";

        public string BuildFilePath(string fileName) => $"{Application.persistentDataPath}/{saveFolderName}/{fileName}";

        public string GetDirectoryPath() => $"{Application.persistentDataPath}/{saveFolderName}";

        /// <summary>
        /// Save a chunk to it's own dedicated file.
        /// </summary>
        /// <param name="chunk">Chunk to save.</param>
        /// <param name="destroyEnemies">If there are enemies to be saved on this chunk, destroy them after saving?</param>
        public void Save(Chunk chunk, bool destroyEnemies) => StartCoroutine(SaveCoroutine(chunk, destroyEnemies));

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

        public IEnumerator SaveCoroutine(Chunk chunk, bool destroyEnemies)
        {
            string chunkFile = BuildChunkFilePath(new Vector3Int((int)chunk.BlockGameObject.transform.position.x,
                                                                 (int)chunk.BlockGameObject.transform.position.y,
                                                                 (int)chunk.BlockGameObject.transform.position.z));
            ValidateDirectory(chunkFile);
            ChunkSaveData newChunkData = new ChunkSaveData(chunk.GetBlockTypeData(), chunk.TreesCreated, GetCharacterData(chunk, destroyEnemies));
            using (var fs = new FileStream(chunkFile, FileMode.Create))
            {
                bf.Serialize(fs, newChunkData);
            }

            yield break;
        }

        private static CharacterData[] GetCharacterData(Chunk chunk, bool destroyEnemies)
        {
            List<CharacterData> characterData = new List<CharacterData>();
            for (int i = 0; i < chunk.Enemies.Count; i++)
            {
                Enemy enemy = chunk.Enemies[i];
                EnemyData enemyData = new EnemyData(enemy.Type, enemy.Health, enemy.transform.position, enemy.transform.rotation);
                if (destroyEnemies)
                {
                    DestroyEnemy(i, enemy);
                }

                characterData.Add(enemyData);
            }

            return characterData.ToArray();

            void DestroyEnemy(int i, Enemy enemy)
            {
                chunk.Enemies.RemoveAt(i);
                DestroyImmediate(enemy.gameObject);
            }
        }

        public void SaveAll()
        {
            PlayerManager.Instance.Save();
            InventoryManager.Instance.Save();
            SaveChunkEnemies();

            void SaveChunkEnemies()
            {
                var chunks = WorldManager.Instance.GetAllChunks();
                for (int i = 0; i < chunks.Count; i++)
                {
                    Chunk chunk = chunks.ElementAt(i).Value;
                    if (chunk.Enemies.Count > 0)
                    {
                        Save(chunk, false);
                    }
                }
            }
        }

        /// <summary>
        /// Load a chunk.
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns>Tuple that includes whether or not the load was succesfull and the chunk's data if so.</returns>
        public (bool, ChunkSaveData) Load(Chunk chunk)
        {
            string chunkFile = BuildChunkFilePath(new Vector3Int((int)chunk.BlockGameObject.transform.position.x,
                                                                 (int)chunk.BlockGameObject.transform.position.y,
                                                                 (int)chunk.BlockGameObject.transform.position.z));
            if (File.Exists(chunkFile))
            {
                ChunkSaveData chunkData;
                using (var fs = new FileStream(chunkFile, FileMode.Open))
                {
                    chunkData = (ChunkSaveData)bf.Deserialize(fs);
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

            return default;
        }

        /// <summary>
        /// Is a particular chunk saved?
        /// </summary>
        /// <param name="chunk"></param>
        public bool Exists(Chunk chunk)
        {
            return File.Exists(BuildChunkFilePath(new Vector3Int((int)chunk.BlockGameObject.transform.position.x,
                                                                 (int)chunk.BlockGameObject.transform.position.y,
                                                                 (int)chunk.BlockGameObject.transform.position.z)));
        }

        /// <summary>
        /// Clear ALL saved files.
        /// </summary>
        public void Clear()
        {
            if (!Directory.Exists(GetDirectoryPath())) return;

            string[] files = Directory.GetFileSystemEntries(GetDirectoryPath(), "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                File.Delete(files[i]);
            }
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