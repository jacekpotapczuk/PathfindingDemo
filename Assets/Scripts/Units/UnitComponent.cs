using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

namespace PathfindingDemo
{
    public enum UnitType
    {
        Player,
        Enemy
    }

    /// <summary>
    /// Core unit behavior handling tile occupancy, movement, and combat interactions.
    /// </summary>
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

        // Movement completion event
        public event Action OnMovementCompleted;

        private void Start()
        {
            movementComponent = GetComponent<UnitMovementComponent>();
            // Subscribe to movement completion event
            movementComponent.OnMovementCompleted += () => OnMovementCompleted?.Invoke();
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

            RemoveFromTile();
            currentTile = tile;
            tile.SetOccupant(this);

            if (tile.TileObject != null)
            {
                var tilePosition = tile.TileObject.transform.position;
                tilePosition.y += 0.5f;
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
            // Clean up event subscription
            if (movementComponent != null)
                movementComponent.OnMovementCompleted -= () => OnMovementCompleted?.Invoke();

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
                animator.enabled = false;

            var rigidbodies = GetComponentsInChildren<Rigidbody>();
            if (rigidbodies.Length == 0)
            {
                var mainRigidbody = gameObject.GetComponent<Rigidbody>();
                if (mainRigidbody == null)
                    mainRigidbody = gameObject.AddComponent<Rigidbody>();
                var fallbackForce = Vector3.up * 5f + UnityEngine.Random.insideUnitSphere * 2f;
                mainRigidbody.AddForce(fallbackForce, ForceMode.Impulse);
                return;
            }

            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = false;
                rb.detectCollisions = true;
            }

            ApplyDeathForces(rigidbodies);
        }

        private void ApplyDeathForces(Rigidbody[] rigidbodies)
        {
            Rigidbody mainTorso = null;
            foreach (var rb in rigidbodies)
            {
                var boneName = rb.name.ToLower();
                if (boneName.Contains("spine") || boneName.Contains("chest") || boneName.Contains("hips"))
                {
                    mainTorso = rb;
                    break;
                }
            }

            if (mainTorso == null && rigidbodies.Length > 0)
                mainTorso = rigidbodies[0];

            if (mainTorso != null)
            {
                var mainForce = Vector3.up * 3f + UnityEngine.Random.insideUnitSphere * 1.5f;
                mainTorso.AddForce(mainForce, ForceMode.Impulse);
                mainTorso.AddTorque(UnityEngine.Random.insideUnitSphere * 5f, ForceMode.Impulse);
            }

            foreach (var rb in rigidbodies)
            {
                if (rb != mainTorso)
                {
                    var randomForce = UnityEngine.Random.insideUnitSphere * 0.5f;
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