using System.Collections.Generic;
using UnityEngine;
using System;

namespace PathfindingDemo
{
    /// <summary>
    /// Manages unit spawning, lifecycle, and combat interactions between player and enemy units.
    /// </summary>
    public class UnitManager : MonoBehaviour
    {
        public static event Action<UnitComponent> OnPlayerUnitSpawned;

        [Header("Unit Prefabs")]
        [SerializeField] private UnitComponent playerUnitPrefab;
        [SerializeField] private UnitComponent enemyUnitPrefab;

        private GridGenerator gridGenerator;
        private UnitComponent playerUnit;
        private List<UnitComponent> activeEnemies = new List<UnitComponent>();

        public UnitComponent PlayerUnit => playerUnit;
        public List<UnitComponent> ActiveEnemies => new List<UnitComponent>(activeEnemies);

        public void Initialize(GridGenerator grid)
        {
            gridGenerator = grid;
        }

        public void SpawnInitialUnits()
        {
            if (gridGenerator?.Grid == null)
            {
                Debug.LogError("UnitManager: Grid not available for unit spawning");
                return;
            }

            if (playerUnit == null)
                SpawnUnit(UnitType.Player);

            if (activeEnemies.Count == 0)
                SpawnUnit(UnitType.Enemy);
        }

        public void SpawnUnit(UnitType unitType)
        {
            var unitPrefab = unitType switch
            {
                UnitType.Enemy => enemyUnitPrefab,
                UnitType.Player => playerUnitPrefab,
                _ => throw new Exception("Invalid UnitType.")
            };

            if (unitPrefab == null)
            {
                Debug.LogWarning($"UnitManager: No prefab assigned for {unitType} unit");
                return;
            }

            var randomTile = gridGenerator.Grid.GetRandomTraversableTile();
            if (randomTile == null)
            {
                Debug.LogError($"UnitManager: No available tiles for {unitType} unit");
                return;
            }

            var unitObject = Instantiate(unitPrefab);
            var unitComponent = unitObject.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                Debug.LogError($"UnitManager: Unit prefab {unitPrefab.name} must have UnitComponent attached");
                return;
            }

            unitComponent.SetTile(randomTile);
            if (unitType == UnitType.Player)
            {
                playerUnit = unitComponent;
                OnPlayerUnitSpawned?.Invoke(playerUnit);
            }
            else if (unitType == UnitType.Enemy)
            {
                activeEnemies.Add(unitComponent);
            }
        }

        public void SpawnNewEnemyImmediately()
        {
            if (enemyUnitPrefab == null)
                return;

            var spawnTile = gridGenerator.Grid.GetRandomTraversableTile();
            if (spawnTile == null)
                return;

            var enemyObject = Instantiate(enemyUnitPrefab);
            var enemyComponent = enemyObject.GetComponent<UnitComponent>();
            if (enemyComponent == null)
            {
                Destroy(enemyObject);
                return;
            }

            enemyComponent.SetTile(spawnTile);
            activeEnemies.Add(enemyComponent);
        }

        public void KillEnemy(UnitComponent enemyUnit)
        {
            if (activeEnemies.Contains(enemyUnit))
                activeEnemies.Remove(enemyUnit);

            enemyUnit.Kill();
            SpawnNewEnemyImmediately();
        }

        public bool IsPlayerUnit(UnitComponent unit)
        {
            return unit == playerUnit;
        }

        public bool IsEnemyUnit(UnitComponent unit)
        {
            return activeEnemies.Contains(unit);
        }
    }
}