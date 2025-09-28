using System.Collections.Generic;
using UnityEngine;

namespace PathfindingDemo
{
    public class UnitManager : MonoBehaviour
    {
        [Header("Unit Prefabs")]
        [SerializeField] private GameObject playerUnitPrefab;
        [SerializeField] private GameObject enemyUnitPrefab;

        private GridGenerator gridGenerator;
        private UnitComponent playerUnit;
        private List<UnitComponent> activeEnemies = new List<UnitComponent>();

        public UnitComponent PlayerUnit => playerUnit;
        public List<UnitComponent> ActiveEnemies => new List<UnitComponent>(activeEnemies);

        public void Initialize(GridGenerator grid)
        {
            gridGenerator = grid;
        }

        public void SpawnUnits()
        {
            if (gridGenerator?.Grid == null)
            {
                Debug.LogError("UnitManager: Grid not available for unit spawning");
                return;
            }

            // Spawn exactly one player unit
            if (playerUnit == null)
            {
                SpawnUnit(playerUnitPrefab, UnitType.Player);
            }

            // Spawn exactly one enemy unit
            if (activeEnemies.Count == 0)
            {
                SpawnUnit(enemyUnitPrefab, UnitType.Enemy);
            }
        }

        public void SpawnUnit(GameObject unitPrefab, UnitType unitType)
        {
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
            }
            else if (unitType == UnitType.Enemy)
            {
                activeEnemies.Add(unitComponent);
            }
        }

        public void SpawnNewEnemyImmediately()
        {
            if (enemyUnitPrefab == null)
            {
                Debug.LogWarning("UnitManager: Cannot spawn new enemy - no enemyUnitPrefab assigned");
                return;
            }

            var spawnTile = gridGenerator.Grid.GetRandomTraversableTile();
            if (spawnTile == null)
            {
                Debug.LogWarning("UnitManager: Cannot spawn new enemy - no available tiles");
                return;
            }

            var enemyObject = Instantiate(enemyUnitPrefab);
            var enemyComponent = enemyObject.GetComponent<UnitComponent>();

            if (enemyComponent == null)
            {
                Debug.LogError($"UnitManager: Enemy prefab {enemyUnitPrefab.name} must have UnitComponent attached");
                Destroy(enemyObject);
                return;
            }

            enemyComponent.SetTile(spawnTile);
            activeEnemies.Add(enemyComponent);

            Debug.Log($"UnitManager: New enemy spawned at {spawnTile.Position}");
        }

        public void KillEnemy(UnitComponent enemyUnit)
        {
            if (activeEnemies.Contains(enemyUnit))
            {
                activeEnemies.Remove(enemyUnit);
            }

            enemyUnit.Kill();
            SpawnNewEnemyImmediately();
            Debug.Log("UnitManager: Enemy killed, new enemy spawned");
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