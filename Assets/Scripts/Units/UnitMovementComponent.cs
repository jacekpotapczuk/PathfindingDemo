using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PathfindingDemo
{
    [RequireComponent(typeof(UnitComponent))]
    public class UnitMovementComponent : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float rotationSmoothTime = 0.08f;
        [SerializeField] private float rotationThreshold = 5f;

        [Header("Audio")]
        [SerializeField] private AudioClip[] footstepAudioClips;
        [SerializeField][Range(0, 1)] private float footstepAudioVolume = 0.3f;

        private UnitComponent unitComponent;
        private Animator animator;
        private bool isMoving = false;
        private Coroutine movementCoroutine;

        // Rotation tracking variables
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;

        // Animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        public bool IsMoving => isMoving;

        private void Start()
        {
            unitComponent = GetComponent<UnitComponent>();
            animator = GetComponent<Animator>();

            if (animator == null)
            {
                Debug.LogWarning($"UnitMovementComponent: No Animator component found on {gameObject.name}");
            }

            AssignAnimationIDs();
        }

        public bool StartMovement(List<TileData> path)
        {
            if (isMoving)
            {
                Debug.LogWarning("UnitMovementComponent: Cannot start movement - unit is already moving");
                return false;
            }

            if (path == null || path.Count < 2)
            {
                Debug.LogWarning("UnitMovementComponent: Cannot start movement - invalid path");
                return false;
            }

            // Validate that we start from the unit's current tile
            if (path[0] != unitComponent.CurrentTile)
            {
                Debug.LogWarning("UnitMovementComponent: Cannot start movement - path doesn't start from current tile");
                return false;
            }

            // Start the movement coroutine
            movementCoroutine = StartCoroutine(MovementCoroutine(path));
            return true;
        }

        public void StopMovement()
        {
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
                movementCoroutine = null;
            }

            isMoving = false;
            SetAnimationSpeed(0f);
        }

        private IEnumerator MovementCoroutine(List<TileData> path)
        {
            isMoving = true;
            SetAnimationSpeed(moveSpeed);

            // Skip the first tile since we're already on it
            for (int i = 1; i < path.Count; i++)
            {
                var targetTile = path[i];

                // Validate the target tile is still walkable and unoccupied
                if (!targetTile.CanBeOccupied())
                {
                    Debug.LogWarning($"UnitMovementComponent: Movement stopped - tile {targetTile.Position} is no longer available");
                    break;
                }

                yield return StartCoroutine(MoveToTile(targetTile));
            }

            // Movement complete
            isMoving = false;
            SetAnimationSpeed(0f);
            movementCoroutine = null;

            Debug.Log($"UnitMovementComponent: Movement completed. Now at {unitComponent.CurrentTile?.Position}");
        }

        private IEnumerator MoveToTile(TileData targetTile)
        {
            if (targetTile?.TileObject == null)
            {
                Debug.LogError("UnitMovementComponent: Cannot move to tile with no TileObject");
                yield break;
            }

            Vector3 startPosition = transform.position;
            Vector3 targetPosition = targetTile.TileObject.transform.position;
            targetPosition.y += 0.5f; // Unit height above tile

            // Calculate target rotation
            Vector3 direction = (targetPosition - startPosition).normalized;
            direction.y = 0; // Keep rotation only on horizontal plane

            if (direction.magnitude > 0.01f)
            {
                _targetRotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

                // Phase 1: Rotation - Stop and rotate toward target
                yield return StartCoroutine(RotateToDirection());
            }

            // Phase 2: Movement - Move forward while maintaining rotation
            yield return StartCoroutine(MoveForward(startPosition, targetPosition));

            // Update tile occupancy through the unit component
            unitComponent.SetTile(targetTile);
        }

        private IEnumerator RotateToDirection()
        {
            // Stop movement animation during rotation
            SetAnimationSpeed(0f);

            while (true)
            {
                float currentRotation = transform.eulerAngles.y;

                // Use SmoothDampAngle for smooth rotation like ThirdPersonController
                float rotation = Mathf.SmoothDampAngle(currentRotation, _targetRotation, ref _rotationVelocity, rotationSmoothTime);

                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

                // Check if rotation is close enough to target
                float angleDifference = Mathf.DeltaAngle(currentRotation, _targetRotation);
                if (Mathf.Abs(angleDifference) < rotationThreshold)
                {
                    // Snap to exact rotation and break
                    transform.rotation = Quaternion.Euler(0.0f, _targetRotation, 0.0f);
                    break;
                }

                yield return null;
            }
        }

        private IEnumerator MoveForward(Vector3 startPosition, Vector3 targetPosition)
        {
            // Start movement animation
            SetAnimationSpeed(moveSpeed);

            float distance = Vector3.Distance(startPosition, targetPosition);
            float moveTime = distance / moveSpeed;
            float elapsedTime = 0f;

            while (elapsedTime < moveTime)
            {
                float t = elapsedTime / moveTime;

                // Only interpolate position, keep rotation fixed
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure we end up exactly at the target
            transform.position = targetPosition;
        }

        private void SetAnimationSpeed(float speed)
        {
            if (!animator) return;

            animator.SetFloat(_animIDSpeed, speed);
            animator.SetFloat(_animIDMotionSpeed, 1f);
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        // Receive event from animation (taken from Unity's ThirdPersonController to avoid errors on footstep notifications)
        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (footstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, footstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(footstepAudioClips[index], transform.TransformPoint(transform.position), footstepAudioVolume);
                }
            }
        }

        private void OnDestroy()
        {
            StopMovement();
        }
    }
}