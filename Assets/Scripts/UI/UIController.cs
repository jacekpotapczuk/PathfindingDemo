using UnityEngine;
using UnityEngine.UI;

namespace PathfindingDemo
{
    /// <summary>
    /// Main UI controller that manages range sliders and enemy spawning for the game interface.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [Header("Slider Controllers")]
        [SerializeField] private SliderController moveRangeSlider;
        [SerializeField] private SliderController attackRangeSlider;
        [SerializeField] private Button spawnEnemyButton;

        private UnitManager unitManager;
        private bool slidersReady = false;
        private UnitComponent cachedPlayerUnit = null;

        private void Start()
        {
            InitializeComponents();
            InitializeSliders();
            SubscribeToEvents();
        }

        private void InitializeComponents()
        {
            unitManager = FindFirstObjectByType<UnitManager>();
            if (unitManager == null)
                Debug.LogError("UIController: UnitManager not found in scene");
        }

        private void InitializeSliders()
        {
            if (moveRangeSlider == null || attackRangeSlider == null)
            {
                Debug.LogError("UIController: SliderControllers not assigned in inspector");
                return;
            }

            moveRangeSlider.OnValueChanged += OnMoveRangeChanged;
            attackRangeSlider.OnValueChanged += OnAttackRangeChanged;
            slidersReady = true;
            TrySetInitialValues();
        }

        private void SubscribeToEvents()
        {
            UnitManager.OnPlayerUnitSpawned += OnPlayerUnitSpawned;
            spawnEnemyButton.onClick.AddListener(OnSpawnEnemyButtonClicked);
        }

        private void OnSpawnEnemyButtonClicked()
        {
            if (unitManager)
                unitManager.SpawnUnit(UnitType.Enemy);
        }

        private void OnPlayerUnitSpawned(UnitComponent playerUnit)
        {
            cachedPlayerUnit = playerUnit;
            TrySetInitialValues();
        }

        private void TrySetInitialValues()
        {
            if (slidersReady && cachedPlayerUnit != null)
            {
                moveRangeSlider.SetValue(cachedPlayerUnit.MoveRange);
                attackRangeSlider.SetValue(cachedPlayerUnit.AttackRange);
            }
        }

        private void OnMoveRangeChanged(int value)
        {
            if (unitManager?.PlayerUnit != null)
            {
                unitManager.PlayerUnit.SetMoveRange(value);
            }
        }

        private void OnAttackRangeChanged(int value)
        {
            if (unitManager?.PlayerUnit != null)
            {
                unitManager.PlayerUnit.SetAttackRange(value);
            }
        }

        private void OnDestroy()
        {
            if (moveRangeSlider != null)
                moveRangeSlider.OnValueChanged -= OnMoveRangeChanged;

            if (attackRangeSlider != null)
                attackRangeSlider.OnValueChanged -= OnAttackRangeChanged;

            UnitManager.OnPlayerUnitSpawned -= OnPlayerUnitSpawned;
            spawnEnemyButton.onClick.RemoveAllListeners();
        }
    }
}