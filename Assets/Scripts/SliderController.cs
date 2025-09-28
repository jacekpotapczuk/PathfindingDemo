using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace PathfindingDemo
{
    public class SliderController : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI valueText;

        [Header("Range Settings")]
        [SerializeField] private int minValue = 1;
        [SerializeField] private int maxValue = 10;

        public event Action<int> OnValueChanged;

        public int Value => Mathf.RoundToInt(slider != null ? slider.value : 0);
        public bool IsValid => slider != null;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (slider == null)
            {
                Debug.LogWarning($"SliderController on {gameObject.name}: Slider not assigned");
                return;
            }

            // Configure slider
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.wholeNumbers = true;

            // Setup event listener
            slider.onValueChanged.AddListener(OnSliderValueChanged);

            // Update text display
            UpdateText(Value);
        }

        public void SetValue(int value)
        {
            if (slider != null)
            {
                slider.value = Mathf.Clamp(value, minValue, maxValue);
                UpdateText(Value);
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        public void Cleanup()
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(OnSliderValueChanged);
            }
        }

        private void OnSliderValueChanged(float value)
        {
            int intValue = Mathf.RoundToInt(value);
            UpdateText(intValue);
            OnValueChanged?.Invoke(intValue);
        }

        private void UpdateText(int value)
        {
            if (valueText != null)
            {
                valueText.text = value.ToString();
            }
        }
    }
}