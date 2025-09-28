using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PathfindingDemo
{
    /// <summary>
    /// Handles unit movement animation and pathfinding along tile-based paths.
    /// </summary>
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
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
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
                Debug.LogWarning($"UnitMovementComponent: No Animator component found on {gameObject.name}");
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

            for (var i = 1; i < path.Count; i++)
            {
                var targetTile = path[i];
                if (!targetTile.CanBeOccupied())
                    break;
                yield return StartCoroutine(MoveToTile(targetTile));
            }

            isMoving = false;
            SetAnimationSpeed(0f);
            movementCoroutine = null;
        }

        private IEnumerator MoveToTile(TileData targetTile)
        {
            if (targetTile?.TileObject == null)
            {
                Debug.LogError("UnitMovementComponent: Cannot move to tile with no TileObject");
                yield break;
            }

            var startPosition = transform.position;
            var targetPosition = targetTile.TileObject.transform.position;
            targetPosition.y += 0.5f;

            var direction = (targetPosition - startPosition).normalized;
            direction.y = 0;

            if (direction.magnitude > 0.01f)
            {
                _targetRotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                yield return StartCoroutine(RotateToDirection());
            }

            yield return StartCoroutine(MoveForward(startPosition, targetPosition));
            unitComponent.SetTile(targetTile);
        }

        private IEnumerator RotateToDirection()
        {
            SetAnimationSpeed(0f);
            while (true)
            {
                var currentRotation = transform.eulerAngles.y;
                var rotation = Mathf.SmoothDampAngle(currentRotation, _targetRotation, ref _rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

                var angleDifference = Mathf.DeltaAngle(currentRotation, _targetRotation);
                if (Mathf.Abs(angleDifference) < rotationThreshold)
                {
                    transform.rotation = Quaternion.Euler(0.0f, _targetRotation, 0.0f);
                    break;
                }
                yield return null;
            }
        }

        private IEnumerator MoveForward(Vector3 startPosition, Vector3 targetPosition)
        {
            SetAnimationSpeed(moveSpeed);
            var distance = Vector3.Distance(startPosition, targetPosition);
            var moveTime = distance / moveSpeed;
            var elapsedTime = 0f;

            while (elapsedTime < moveTime)
            {
                var t = elapsedTime / moveTime;
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

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