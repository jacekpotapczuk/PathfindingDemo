using UnityEngine;
using UnityEngine.UI;

namespace PathfindingDemo
{
    public class UIController : MonoBehaviour
    {
        [Header("Slider Controllers")]
        [SerializeField] private SliderController moveRangeSlider;
        [SerializeField] private SliderController attackRangeSlider;
        [SerializeField] private Button spawnEnemyButton;

        private UnitManager unitManager;

        // Initialization flags
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
            {
                Debug.LogError("UIController: UnitManager not found in scene");
            }
        }

        private void InitializeSliders()
        {
            if (moveRangeSlider == null || attackRangeSlider == null)
            {
                Debug.LogError("UIController: SliderControllers not assigned in inspector");
                return;
            }

            // SliderControllers will initialize themselves in their Start() method
            // We just need to set up our event listeners and set initial values

            // Setup event listeners
            moveRangeSlider.OnValueChanged += OnMoveRangeChanged;
            attackRangeSlider.OnValueChanged += OnAttackRangeChanged;

            // Mark sliders as ready
            slidersReady = true;

            // Try to set initial values if player unit is already available
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
            {
                unitManager.SpawnUnit(UnitType.Enemy);   
            }
        }

        private void OnPlayerUnitSpawned(UnitComponent playerUnit)
        {
            cachedPlayerUnit = playerUnit;
            TrySetInitialValues();
        }

        private void TrySetInitialValues()
        {
            // Only set values when both sliders are ready AND player unit is available
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
            // Clean up event listeners
            if (moveRangeSlider != null)
            {
                moveRangeSlider.OnValueChanged -= OnMoveRangeChanged;
            }

            if (attackRangeSlider != null)
            {
                attackRangeSlider.OnValueChanged -= OnAttackRangeChanged;
            }

            // Unsubscribe from static events
            UnitManager.OnPlayerUnitSpawned -= OnPlayerUnitSpawned;
            
            spawnEnemyButton.onClick.RemoveAllListeners();
        }
    }
}