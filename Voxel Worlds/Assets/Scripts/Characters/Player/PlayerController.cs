using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Voxel.Utility;
using Voxel.World;

namespace Voxel.Player
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField]
        private InputActionsController inputActionsController = default;
        private CharacterController characterController;
        private Transform playerCamera;
        private Transform player;

        [Header("Grounding and Gravity")]
        [SerializeField]
        private PlayerGroundStateVariable groundState = default;
        [SerializeField]
        private LayerMask layersToDetect = default;
        [SerializeField]
        private float maxRayDistance = 1.25f;
        [SerializeField]
        private float baseGravityMultiplier = 2;
        [SerializeField]
        private float distanceMultiplierMax = 5;

        [Header("Moving")]
        [SerializeField]
        private PlayerMoveStateVariable moveState = default;
        private Vector2 moveValue;
        [SerializeField]
        private float moveSpeed = 5;
        [SerializeField]
        private float sprintSpeedMultiplier = 1.5f;
        private float originalMoveSpeed;

        [Header("Block Collision")]
        [SerializeField]
        private Transform rayStart = default;
        [SerializeField]
        private float blockCollisionRayLength = 0.75f;
        [SerializeField]
        private float leftRightRayAngle = 20;

        [Header("Looking")]
        [SerializeField]
        private PlayerLookStateVariable lookState = default;
        private Vector2 lookValue;
        [SerializeField]
        private float lookSpeedMultiplier = 5;
        [SerializeField]
        private float lookVerticalClampRange = 75;
        private float lookValueX;
        private float lookValueY;

        [Header("Jumping")]
        [SerializeField]
        private PlayerJumpStateVariable jumpState = default;
        [SerializeField, Tooltip("Essentially the same as jump speed.")]
        private float jumpStartValue = 0.2f;
        [SerializeField]
        private float newJumpDelay = 0.2f;
        [SerializeField]
        private float jumpReduceAmount = 0.0075f;
        [SerializeField]
        private float jumpTotalMultiplier = 15;
        private WaitForSeconds jumpDelayWFS;
        private Vector3 jumpVector;

        private void Awake()
        {
            playerCamera = GetComponentInChildren<Camera>().transform;
            characterController = GetComponent<CharacterController>();
            player = transform;
            originalMoveSpeed = moveSpeed;
            jumpDelayWFS = new WaitForSeconds(newJumpDelay);
        }

        private void OnEnable()
        {
            inputActionsController.InputActions.Player.Move.performed += OnMovePerformed;
            inputActionsController.InputActions.Player.Move.canceled += OnMoveCanceled;
            inputActionsController.InputActions.Player.Look.performed += OnLookPerformed;
            inputActionsController.InputActions.Player.Look.canceled += OnLookCanceled;
            inputActionsController.InputActions.Player.Sprint.performed += OnSprintPerformed;
            inputActionsController.InputActions.Player.Sprint.canceled += OnSprintCanceled;
            inputActionsController.InputActions.Player.Jump.performed += OnJumpPerformed;
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
            lookValue = context.ReadValue<Vector2>();
            lookState.Value = PlayerLookState.IsLooking;
        }

        private void OnLookCanceled(InputAction.CallbackContext context)
            => lookState.Value = PlayerLookState.None;

        private void OnSprintPerformed(InputAction.CallbackContext context)
            => moveSpeed *= sprintSpeedMultiplier;

        private void OnSprintCanceled(InputAction.CallbackContext context)
            => moveSpeed = originalMoveSpeed;

        private void OnJumpPerformed(InputAction.CallbackContext context)
            => Jump();

        private void Update()
        {
            Vector3 totalMoveValue = Vector3.zero;
            totalMoveValue.y += Grounding();
            totalMoveValue += Moving();

            bool isJumping = false;
            if (jumpState.Value == PlayerJumpState.IsJumping)
            {
                isJumping = true;
                totalMoveValue += jumpVector;
            }

            if (!IsCollidingWithBlock(totalMoveValue)
                || isJumping)
            {
                characterController.Move(totalMoveValue * Time.deltaTime);
            }
        }

        private float Grounding()
        {
            float rayLength = WorldManager.Instance.MaxWorldHeight * 2;
            Physics.Raycast(player.position, Vector3.down, out RaycastHit hitInfo, rayLength, layersToDetect);
            if (hitInfo.collider != null
                && hitInfo.distance <= maxRayDistance)
            {
                groundState.Value = PlayerGroundState.IsGrounded;
                return 0;
            }

            float distanceMultiplier;
            if (hitInfo.collider != null)
            {
                distanceMultiplier = Mathf.Clamp(hitInfo.distance, distanceMultiplierMax / 4, distanceMultiplierMax);
            }
            else
            {
                distanceMultiplier = distanceMultiplierMax;
            }

            groundState.Value = PlayerGroundState.None;
            return -Mathf.Abs(baseGravityMultiplier * distanceMultiplier);
        }

        private Vector3 Moving()
        {
            if (moveState.Value == PlayerMoveState.IsMoving)
            {
                Vector3 moveX = (GetPlayerSideways() * moveValue.x).normalized;
                Vector3 moveZ = (GetPlayerForward() * moveValue.y).normalized;
                return (moveX + moveZ) * moveSpeed;
            }

            return Vector3.zero;
        }

        private void Jump()
        {
            if (jumpState.Value == PlayerJumpState.IsJumping) return;

            StartCoroutine(JumpCoroutine());
        }

        private IEnumerator JumpCoroutine()
        {
            jumpState.Value = PlayerJumpState.IsJumping;
            float time = jumpStartValue;
            while (time > 0)
            {
                time -= jumpReduceAmount;
                jumpVector = new Vector3(0, Mathf.Clamp(time, 0, jumpStartValue), 0) * jumpTotalMultiplier;
                yield return null;
            }

            yield return jumpDelayWFS;
            jumpState.Value = PlayerJumpState.None;
        }

        private bool IsCollidingWithBlock(Vector3 totalMoveValue)
        {
            Vector3 playerForward = GetPlayerForward();
            // Forward ray
            Physics.Raycast(rayStart.position, playerForward, out RaycastHit forwardHitInfo, blockCollisionRayLength, layersToDetect);
            // Right ray
            Vector3 playerForwardRight = Quaternion.AngleAxis(leftRightRayAngle, Vector3.up) * playerForward;
            Physics.Raycast(rayStart.position, playerForwardRight, out RaycastHit rightHitInfo, blockCollisionRayLength, layersToDetect);
            // Left ray
            Vector3 playerForwardLeft = Quaternion.AngleAxis(-leftRightRayAngle, Vector3.up) * playerForward;
            Physics.Raycast(rayStart.position, playerForwardLeft, out RaycastHit leftHitInfo, blockCollisionRayLength, layersToDetect);

            return (forwardHitInfo.collider != null
                   || rightHitInfo.collider != null
                   || leftHitInfo.collider != null)
                   && !Mathf.Approximately(totalMoveValue.z, 0)
                   && !Mathf.Approximately(totalMoveValue.x, 0);
        }

        private Vector3 GetPlayerForward() => new Vector3(playerCamera.forward.x, 0, playerCamera.forward.z);

        private Vector3 GetPlayerSideways() => new Vector3(playerCamera.right.x, 0, playerCamera.right.z);

        private void LateUpdate() => Looking();

        private void Looking()
        {
            if (lookState.Value == PlayerLookState.IsLooking)
            {
                CalculateLookValue();
                ApplyLookValue();
            }
        }

        private void CalculateLookValue()
        {
            lookValue *= lookSpeedMultiplier * Time.deltaTime;
            lookValueX += lookValue.x;
            lookValueY -= lookValue.y;
            lookValueY = Mathf.Clamp(lookValueY, -lookVerticalClampRange, lookVerticalClampRange);
        }

        private void ApplyLookValue()
        {
            player.localRotation = Quaternion.Euler(0, lookValueX, 0);
            playerCamera.localRotation = Quaternion.Euler(lookValueY, 0, 0);
        }

        private void OnDisable()
        {
            inputActionsController.InputActions.Player.Move.performed -= OnMovePerformed;
            inputActionsController.InputActions.Player.Move.canceled -= OnMoveCanceled;
            inputActionsController.InputActions.Player.Look.performed -= OnLookPerformed;
            inputActionsController.InputActions.Player.Look.canceled -= OnLookCanceled;
            inputActionsController.InputActions.Player.Sprint.performed -= OnSprintPerformed;
            inputActionsController.InputActions.Player.Sprint.canceled -= OnSprintCanceled;
            inputActionsController.InputActions.Player.Jump.performed -= OnJumpPerformed;
        }
    }
}