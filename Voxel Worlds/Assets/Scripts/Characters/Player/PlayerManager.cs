using UnityEngine;
using Voxel.Characters.Saving;
using Voxel.Items.Inventory;
using Voxel.Saving;
using Voxel.Utility;
using Voxel.World;

namespace Voxel.Player
{
    public class PlayerManager : Singleton<PlayerManager>
    {
        [SerializeField]
        private GameObject playerPrefab = default;
        public Transform ActivePlayer { get; private set; }
        public Player Player { get; set; }

        public CharacterController CharacterController { get; private set; }

        [SerializeField]
        private Canvas mainUICanvas = default;

        [SerializeField]
        private float playerSpawnOffset = 1;
        [SerializeField]
        private int getPositionRadius = 10;
        [SerializeField]
        private int getPositionMinBounds = 5;
        [SerializeField]
        private int minSpawnHeight = 10;
        [SerializeField]
        private int maxSpawnTries = 10;
        [SerializeField]
        private float rayHitSpawnOffset = 2;
        [SerializeField]
        private float cameraPlaneRenderDistance = 0.1f;
        [SerializeField]
        private string playerSaveFileName = "PlayerData.dat";

        public bool PlayerLoaded { get; private set; }

        /// <summary>
        /// Contains the loaded position if one exists, otherwise contains a default raycast position to determine initial player position.
        /// </summary>
        public Vector3 InitialPosition { get; private set; }

        /// <summary>
        /// Contains the loaded rotation if one exists, otherwise contains a default identity rotation.
        /// </summary>
        public Quaternion InitialRotation { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            InitialPosition = new Vector3(0, WorldManager.Instance.MaxWorldHeight, 0);
            InitialRotation = Quaternion.identity;
        }

        public void Save()
        {
            SaveManager.Instance.Save(new CharacterData(ActivePlayer.position, ActivePlayer.rotation),
                                      SaveManager.Instance.BuildFilePath(playerSaveFileName));
        }

        public void Load()
        {
            (bool loaded, CharacterData playerData) = SaveManager.Instance.Load<CharacterData>(SaveManager.Instance.BuildFilePath(playerSaveFileName));
            PlayerLoaded = loaded;
            if (PlayerLoaded)
            {
                InitialPosition = playerData.Position;
                InitialRotation = playerData.Rotation;
            }
        }

        public void RespawnAndResetPlayer()
        {
            Player.Health = Player.StartingHealth;
            ActivePlayer.position = GetPositionInRadius(ActivePlayer.position, getPositionMinBounds, getPositionRadius);
        }

        public Transform SpawnPlayer()
        {
            if (PlayerLoaded)
            {
                return InitializeAndSpawnPlayer(InitialPosition, InitialRotation);
            }

            return InitializeAndSpawnPlayer(GetPositionInRadius(Vector3.zero, getPositionMinBounds, getPositionRadius), Quaternion.identity);
        }

        private Transform InitializeAndSpawnPlayer(Vector3 position, Quaternion rotation)
        {
            GameObject player = Instantiate(playerPrefab, position, rotation);
            ActivePlayer = player.transform;
            Player = ActivePlayer.GetComponent<Player>();
            CharacterController = ActivePlayer.GetComponent<CharacterController>();
            InitializeUI();
            InventoryManager.Instance.Load();
            return ActivePlayer;
        }

        /// <summary>
        /// Get a position in radius where the hit point has a game object with a specific keyword in the name.
        /// </summary>
        public Vector3 GetPositionInRadius(Vector3 startPos, int minBounds, int radius)
        {
            Vector2 randomCoord = Random.insideUnitCircle * radius;
            randomCoord.x += minBounds;
            randomCoord.y += minBounds;
            startPos.x += RandomiseNegative(randomCoord.x);
            startPos.z += RandomiseNegative(randomCoord.y);

            Vector3 rayPos = new Vector3(startPos.x, WorldManager.Instance.MaxWorldHeight, startPos.z);
            Physics.Raycast(rayPos, Vector3.down, out RaycastHit hitInfo, rayPos.y * rayHitSpawnOffset);
            int tries = 0;
            while (hitInfo.collider == null
                   || hitInfo.collider.transform.position.y < minSpawnHeight
                   || tries <= maxSpawnTries)
            {
                tries++;
                Physics.Raycast(rayPos, Vector3.down, out hitInfo, rayPos.y * rayHitSpawnOffset);
            }

            return hitInfo.point + new Vector3(0, playerSpawnOffset, 0);
        }

        private float RandomiseNegative(float value) => Random.Range(0, 2) == 0? -value : value;

        private void InitializeUI()
        {
            mainUICanvas.renderMode = RenderMode.ScreenSpaceCamera;
            mainUICanvas.worldCamera = ReferenceManager.Instance.MainCamera;
            mainUICanvas.planeDistance = cameraPlaneRenderDistance;
        }
    }
}