using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Voxel.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Misc. Dependencies")]
        private InputActions inputActions;
        private CharacterController characterController;
        private Transform playerCamera;
        private Transform player;

        [Header("Grounding and Gravity")]
        [SerializeField]
        private float rayDistance = 1;
        [SerializeField]
        private float gravityMultiplier = 2;
        private bool isGrounded;

        [Header("Moving")]
        private Vector2 moveValue;
        [SerializeField]
        private float moveSpeed = 5;
        [SerializeField]
        private float sprintSpeedMultiplier = 1.5f;
        private float originalMoveSpeed;
        private bool move;

        [Header("Looking")]
        private Vector2 lookValue;
        [SerializeField]
        private float lookSpeed = 5;
        private bool look;

        [Header("Jumping")]
        private Coroutine jumpCoroutine;
        [SerializeField]
        private float jumpMaxHeight = 2;
        [SerializeField]
        private float jumpMaxTime = 1;
        [SerializeField]
        private float jumpStartSpeed = 10;
        [SerializeField]
        private float jumpSpeedReduction = 5;
        private bool isJumping;

        private void Awake()
        {
            inputActions = new InputActions();
            playerCamera = GetComponentInChildren<Camera>().transform;
            characterController = GetComponent<CharacterController>();
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
            inputActions.Player.Jump.performed += OnJumpPerformed;
            inputActions.Player.Enable();
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            moveValue = context.ReadValue<Vector2>();
            move = true;
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
            => move = false;

        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            lookValue = context.ReadValue<Vector2>();
            look = true;
        }

        private void OnLookCanceled(InputAction.CallbackContext context)
            => look = false;

        private void OnSprintPerformed(InputAction.CallbackContext context)
            => moveSpeed *= sprintSpeedMultiplier;

        private void OnSprintCanceled(InputAction.CallbackContext context)
            => moveSpeed = originalMoveSpeed;

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            Jump();
        }

        private void Update()
        {
            Ground();
            Move();
            Look();
        }

        private void Ground()
        {
            bool hit = Physics.Raycast(player.position, Vector3.down, out RaycastHit hitInfo, rayDistance);
            if (hit)
            {
                isGrounded = true;
            }
            else
            {
                isGrounded = false;
                Gravity(hitInfo.distance);
            }
        }

        private void Gravity(float distanceFromGround)
        {
            Vector3 gravity = new Vector3(0, (Physics.gravity / Mathf.Pow(distanceFromGround, 2) * gravityMultiplier).y, 0);
            characterController.SimpleMove(gravity);
        }

        private void Jump()
        {
            if (isJumping) return;
            else if (jumpCoroutine != null)
            {
                StopCoroutine(jumpCoroutine);
            }

            jumpCoroutine = StartCoroutine(JumpCoroutine());
        }

        private IEnumerator JumpCoroutine()
        {
            isJumping = true;
            float goalPlayerHeight = player.position.y + jumpMaxHeight;
            float jumpMaxTimeLength = Time.realtimeSinceStartup + jumpMaxTime;
            float jumpMoveSpeed = jumpStartSpeed;
            while(player.position.y < goalPlayerHeight && Time.realtimeSinceStartup < jumpMaxTimeLength)
            {
                Vector3 jumpVector = new Vector3(player.position.x, goalPlayerHeight, player.position.z);
                player.position = Vector3.MoveTowards(player.position, jumpVector, jumpMoveSpeed * Time.deltaTime);
                jumpMoveSpeed -= jumpSpeedReduction * Time.deltaTime;
                yield return null;
            }

            isJumping = false;
        }

        private void Move()
        {
            if (move)
            {
                Vector3 moveX = playerCamera.right * moveValue.x;
                Vector3 moveY = playerCamera.forward * moveValue.y;
                Vector3 finalMove = (moveX + moveY) * moveSpeed * Time.deltaTime;
                characterController.Move(finalMove);
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

        private void OnDisable()
        {
            inputActions.Player.Move.performed -= OnMovePerformed;
            inputActions.Player.Move.canceled -= OnMoveCanceled;
            inputActions.Player.Look.performed -= OnLookPerformed;
            inputActions.Player.Look.canceled -= OnLookCanceled;
            inputActions.Player.Sprint.performed -= OnSprintPerformed;
            inputActions.Player.Sprint.canceled -= OnSprintCanceled;
            inputActions.Player.Jump.performed -= OnJumpPerformed;
            inputActions.Player.Disable();
        }
    }
}