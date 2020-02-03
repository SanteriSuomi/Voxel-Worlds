using UnityEngine;
using UnityEngine.InputSystem;

namespace Voxel.Player
{
    public class CameraController : MonoBehaviour
    {
        private InputActions inputActions;

        [SerializeField]
        private Transform player = default;
        private Transform playerCamera;

        private Vector2 moveValue;
        [SerializeField]
        private float moveSpeed = 5;
        private float originalMoveSpeed;
        private bool move;

        [SerializeField]
        private float sprintSpeedMultiplier = 1.5f;

        private Vector2 lookValue;
        [SerializeField]
        private float lookSpeed = 5;
        private bool look;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            inputActions = new InputActions();
            playerCamera = transform;
            originalMoveSpeed = moveSpeed;
        }

        private void OnEnable()
        {
            inputActions.Player.Move.performed += OnMovePerformed;
            inputActions.Player.Move.canceled += OnMoveCanceled;
            inputActions.Player.Look.performed += OnLookPerformed;
            inputActions.Player.Look.canceled += OnLookCanceled;
            inputActions.Player.Sprint.performed += OnSprintPerformed;
            inputActions.Player.Sprint.canceled += OnSprintCanceled;
            inputActions.Player.Enable();
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            moveValue = context.ReadValue<Vector2>();
            move = true;
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            move = false;
        }

        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            lookValue = context.ReadValue<Vector2>();
            look = true;
        }

        private void OnLookCanceled(InputAction.CallbackContext context)
        {
            look = false;
        }

        private void OnSprintPerformed(InputAction.CallbackContext context)
        {
            moveSpeed *= sprintSpeedMultiplier;
        }

        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            moveSpeed = originalMoveSpeed;
        }

        private void Update()
        {
            Move();
            Look();
        }

        private void Move()
        {
            if (move)
            {
                Vector3 moveX = playerCamera.right * moveValue.x;
                Vector3 moveY = playerCamera.forward * moveValue.y;
                Vector3 finalMove = moveX + moveY;
                player.position = Vector3.Lerp(player.position, player.position + finalMove, moveSpeed * Time.deltaTime);
            }
        }

        private void Look()
        {
            if (look)
            {
                Quaternion cameraRotation = playerCamera.rotation * Quaternion.Euler(new Vector3(-lookValue.y, 0, 0));
                playerCamera.rotation = Quaternion.Slerp(playerCamera.rotation, cameraRotation, lookSpeed * Time.deltaTime);
                Quaternion playerRotation = player.rotation * Quaternion.Euler(new Vector3(0, lookValue.x, 0));
                player.rotation = Quaternion.Slerp(player.rotation, playerRotation, lookSpeed * Time.deltaTime);
            }
        }
    }
}