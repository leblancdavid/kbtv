using System;
using UnityEngine;

namespace KBTV.Data
{
    /// <summary>
    /// Represents a single stat meter (0-100) with clamping and change events.
    /// </summary>
    [Serializable]
    public class Stat
    {
        [SerializeField] private string _name;
        [SerializeField] private float _value;
        [SerializeField] private float _minValue = 0f;
        [SerializeField] private float _maxValue = 100f;

        public string Name => _name;
        public float Value => _value;
        public float MinValue => _minValue;
        public float MaxValue => _maxValue;
        public float Normalized => (_value - _minValue) / (_maxValue - _minValue);

        public event Action<float, float> OnValueChanged; // oldValue, newValue

        public Stat(string name, float initialValue = 50f, float min = 0f, float max = 100f)
        {
            _name = name;
            _minValue = min;
            _maxValue = max;
            _value = Mathf.Clamp(initialValue, min, max);
        }

        public void SetValue(float newValue)
        {
            float oldValue = _value;
            _value = Mathf.Clamp(newValue, _minValue, _maxValue);
            
            if (!Mathf.Approximately(oldValue, _value))
            {
                OnValueChanged?.Invoke(oldValue, _value);
            }
        }

        public void Modify(float delta)
        {
            SetValue(_value + delta);
        }

        public void Reset(float value)
        {
            float oldValue = _value;
            _value = Mathf.Clamp(value, _minValue, _maxValue);
            
            if (!Mathf.Approximately(oldValue, _value))
            {
                OnValueChanged?.Invoke(oldValue, _value);
            }
        }

        public bool IsEmpty => Mathf.Approximately(_value, _minValue);
        public bool IsFull => Mathf.Approximately(_value, _maxValue);
    }
}
