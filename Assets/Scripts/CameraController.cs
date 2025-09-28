using UnityEngine;

namespace PathfindingDemo
{
    /// <summary>
    /// Simple camera movement controller using WASD keys for grid navigation.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 10f;

        private Camera playerCamera;

        private void Start()
        {
            playerCamera = GetComponent<Camera>();
            if (playerCamera == null)
                playerCamera = Camera.main;
        }

        private void Update()
        {
            HandleCameraMovement();
        }

        private void HandleCameraMovement()
        {
            var moveDirection = Vector3.zero;

            if (Input.GetKey(KeyCode.W))
                moveDirection += Vector3.forward;
            if (Input.GetKey(KeyCode.S))
                moveDirection += Vector3.back;
            if (Input.GetKey(KeyCode.A))
                moveDirection += Vector3.left;
            if (Input.GetKey(KeyCode.D))
                moveDirection += Vector3.right;

            if (moveDirection != Vector3.zero)
            {
                moveDirection.Normalize();
                var currentPosition = transform.position;
                var newPosition = currentPosition + moveDirection * moveSpeed * Time.deltaTime;
                newPosition.y = currentPosition.y;
                transform.position = newPosition;
            }
        }
    }
}