using UnityEngine;
using UnityEngine.InputSystem;

namespace Voxel.Player
{
    public class PlayerController : MonoBehaviour
    {
        private InputActions inputActions;

        [Header("Transforms")]
        [SerializeField]
        private Transform playerCamera = default;
        private Transform player;

        [Header("Grounding and Gravity")]
        [SerializeField]
        private LayerMask groundedLayerMask = default;
        [SerializeField]
        private float groundedRayMaxDistance = 100;
        [SerializeField]
        private float isGroundedDistance = 0.1f;
        [SerializeField]
        private float gravityMultiplier = 2;

        [Header("Moving")]
        private Vector2 moveValue;
        [SerializeField]
        private float moveSpeed = 5;
        private float originalMoveSpeed;
        private bool move;
        [SerializeField]
        private float sprintSpeedMultiplier = 1.5f;

        [Header("Looking")]
        private Vector2 lookValue;
        [SerializeField]
        private float lookSpeed = 5;
        private bool look;

        private void Awake()
        {
            inputActions = new InputActions();
            player = transform;
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
            Gravity();
            Jump();
            Move();
            Look();
        }

        private void Gravity()
        {
            Physics.Raycast(player.position, Vector3.down, out RaycastHit hitInfo, groundedRayMaxDistance, groundedLayerMask);
            if (hitInfo.collider == null || hitInfo.distance > isGroundedDistance)
            {
                Debug.Log("gravity");
                player.position += new Vector3(0, (Physics.gravity / Mathf.Pow(hitInfo.distance, 2) * gravityMultiplier).y, 0);
            }
        }

        private void Jump()
        {

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