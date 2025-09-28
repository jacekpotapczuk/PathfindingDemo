using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace PathfindingDemo
{
    public enum UnitType
    {
        Player,
        Enemy
    }

    [RequireComponent(typeof(UnitMovementComponent))]
    public class UnitComponent : MonoBehaviour, ITileOccupant
    {
        [Header("Unit Settings")]
        [SerializeField] private UnitType unitType = UnitType.Player;
        [SerializeField] private int moveRange = 4;
        [SerializeField] private int attackRange = 4;

        private TileData currentTile;
        private UnitMovementComponent movementComponent;

        public UnitType Type => unitType;
        public int MoveRange => moveRange;
        public int AttackRange => attackRange;
        public TileData CurrentTile => currentTile;
        public bool IsMoving => movementComponent.IsMoving;

        private void Start()
        {
            movementComponent = GetComponent<UnitMovementComponent>();
        }

        public bool CanOccupyTile(TileData tile)
        {
            return tile != null && tile.CanBeOccupied();
        }

        public void SetTile(TileData tile)
        {
            if (tile == null)
            {
                Debug.LogError("Cannot set unit to null tile");
                return;
            }

            if (!CanOccupyTile(tile))
            {
                Debug.LogError($"Unit cannot occupy tile at {tile.Position}");
                return;
            }

            // Remove from current tile if we have one
            RemoveFromTile();

            // Set new tile
            currentTile = tile;
            tile.SetOccupant(this);

            // Position the unit GameObject on the tile
            if (tile.TileObject != null)
            {
                Vector3 tilePosition = tile.TileObject.transform.position;
                tilePosition.y += 0.5f; // Place unit above the tile
                transform.position = tilePosition;
            }
        }

        public void RemoveFromTile()
        {
            if (currentTile != null)
            {
                currentTile.RemoveOccupant();
                currentTile = null;
            }
        }

        public void SetMoveRange(int range)
        {
            moveRange = Mathf.Max(0, range);
        }

        public void SetAttackRange(int range)
        {
            attackRange = Mathf.Max(0, range);
        }

        public bool StartMovement(List<TileData> path)
        {
            return movementComponent.StartMovement(path);
        }

        public void StopMovement()
        {
            movementComponent.StopMovement();
        }

        private void OnDestroy()
        {
            RemoveFromTile();
        }

        public void Kill()
        {
            StopMovement();
            RemoveFromTile();
            ActivateRagdoll();
            Destroy(gameObject, 2f);
        }

        private void ActivateRagdoll()
        {
            var animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }

            var rigidbodies = GetComponentsInChildren<Rigidbody>();

            if (rigidbodies.Length == 0)
            {
                var mainRigidbody = gameObject.GetComponent<Rigidbody>();
                if (mainRigidbody == null)
                {
                    mainRigidbody = gameObject.AddComponent<Rigidbody>();
                }
                var fallbackForce = Vector3.up * 5f + Random.insideUnitSphere * 2f;
                mainRigidbody.AddForce(fallbackForce, ForceMode.Impulse);
                return;
            }

            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = false;
                rb.detectCollisions = true;
            }

            ApplyDeathForces(rigidbodies);
            Debug.Log($"Ragdoll activated with {rigidbodies.Length} rigidbodies");
        }

        private void ApplyDeathForces(Rigidbody[] rigidbodies)
        {
            Rigidbody mainTorso = null;

            foreach (var rb in rigidbodies)
            {
                string boneName = rb.name.ToLower();
                if (boneName.Contains("spine") || boneName.Contains("chest") || boneName.Contains("hips"))
                {
                    mainTorso = rb;
                    break;
                }
            }

            if (mainTorso == null && rigidbodies.Length > 0)
            {
                mainTorso = rigidbodies[0];
            }

            if (mainTorso != null)
            {
                Vector3 mainForce = Vector3.up * 3f + Random.insideUnitSphere * 1.5f;
                mainTorso.AddForce(mainForce, ForceMode.Impulse);
                mainTorso.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            }

            foreach (var rb in rigidbodies)
            {
                if (rb != mainTorso)
                {
                    Vector3 randomForce = Random.insideUnitSphere * 0.5f;
                    rb.AddForce(randomForce, ForceMode.Impulse);
                }
            }
        }

        public override string ToString()
        {
            var tileInfo = currentTile != null ? $" on {currentTile.Position}" : " (no tile)";
            var movingInfo = IsMoving ? " [Moving]" : "";
            return $"{unitType} Unit{tileInfo} - Move:{moveRange}, Attack:{attackRange}{movingInfo}";
        }
    }
}