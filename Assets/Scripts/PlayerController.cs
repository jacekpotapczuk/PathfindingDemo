using UnityEngine;

namespace PathfindingDemo
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private LayerMask tileLayerMask = -1;

        private Camera playerCamera;
        private TileComponent hoveredTileComponent;
        private GridGenerator gridGenerator;

        private void Start()
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindFirstObjectByType<Camera>();
            }

            gridGenerator = FindFirstObjectByType<GridGenerator>();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void Update()
        {
            UpdateHoveredTile();
            HandleInput();
        }

        private void UpdateHoveredTile()
        {
            if (playerCamera == null) return;

            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, tileLayerMask))
            {
                var tileComponent = hit.collider.GetComponent<TileComponent>();
                if (tileComponent != null)
                {
                    hoveredTileComponent = tileComponent;
                }
                else
                {
                    hoveredTileComponent = null;
                }
            }
            else
            {
                hoveredTileComponent = null;
            }
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0) && hoveredTileComponent != null)
            {
                hoveredTileComponent.CycleTileType();
            }
        }
    }
}