using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Voxel.Utility;

namespace Voxel.Player
{
    public class PlayerController : MonoBehaviour
    {
        private InputActions inputActions;
        private CharacterController characterController;
        private Transform playerCamera;
        private Transform player;

        [Header("Grounding and Gravity")]
        [SerializeField]
        private PlayerGroundStateVariable groundState = default;
        [SerializeField]
        private float rayDistance = 1;
        [SerializeField]
        private float gravityMultiplier = 2;

        [Header("Moving")]
        [SerializeField]
        private PlayerMoveStateVariable moveState = default;
        private Vector2 moveValue;
        [SerializeField]
        private float moveSpeed = 5;
        [SerializeField]
        private float sprintSpeedMultiplier = 1.5f;
        private float originalMoveSpeed;

        [Header("Looking")]
        [SerializeField]
        private PlayerLookStateVariable lookState = default;
        [SerializeField]
        private float lookSpeedMultiplier = 5;
        [SerializeField]
        private float lookVerticalClampRange = 75;
        private float lookValueX;
        private float lookValueY;

        [Header("Jumping")]
        [SerializeField]
        private PlayerJumpStateVariable jumpState = default;
        private Coroutine jumpCoroutine;
        [SerializeField]
        private float jumpMaxHeight = 2;
        [SerializeField]
        private float jumpMaxTime = 1;
        [SerializeField]
        private float jumpStartSpeed = 10;
        [SerializeField]
        private float jumpSpeedReduction = 5;

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
            moveState.Value = PlayerMoveState.IsMoving;
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
            => moveState.Value = PlayerMoveState.None;

        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            Vector2 lookValue = context.ReadValue<Vector2>();
            lookValue *= lookSpeedMultiplier * Time.deltaTime;
            lookValueX += lookValue.x;
            lookValueY -= lookValue.y;
            lookState.Value = PlayerLookState.IsLooking;
        }

        private void OnLookCanceled(InputAction.CallbackContext context)
            => lookState.Value = PlayerLookState.None;

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
                groundState.Value = PlayerGroundState.IsGrounded;
            }
            else
            {
                groundState.Value = PlayerGroundState.None;
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
            if (jumpState.Value == PlayerJumpState.IsJumping) return;
            else if (jumpCoroutine != null)
            {
                StopCoroutine(jumpCoroutine);
            }

            jumpCoroutine = StartCoroutine(JumpCoroutine());
        }

        private IEnumerator JumpCoroutine()
        {
            jumpState.Value = PlayerJumpState.IsJumping;
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

            jumpState.Value = PlayerJumpState.None;
        }

        private void Move()
        {
            if (moveState.Value == PlayerMoveState.IsMoving)
            {
                Vector3 moveX = playerCamera.right * moveValue.x;
                Vector3 moveY = playerCamera.forward * moveValue.y;
                Vector3 finalMove = (moveX + moveY) * moveSpeed * Time.deltaTime;
                characterController.Move(finalMove);
            }
        }

        private void Look()
        {
            if (lookState.Value == PlayerLookState.IsLooking)
            {
                player.localRotation = Quaternion.Euler(0, lookValueX, 0);
                lookValueY = Utils.FastClamp(lookValueY, -lookVerticalClampRange, lookVerticalClampRange);
                playerCamera.localRotation = Quaternion.Euler(lookValueY, 0, 0);
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