using System;
using UnityEngine;

public class PlayerStat : StatBase
{
    [SerializeField] private float _maxHp = 100f;
    [SerializeField] private float _maxStamina = 100f;

    public float MaxStamina => _maxStamina;
    public float CurrentStamina { get; private set; }

    public event Action<float, float> OnStaminaChanged;

    private void Awake()
    {
        MaxHp = _maxHp;
        CurrentHp = _maxHp;
        CurrentStamina = _maxStamina;
    }

    public void Heal(float amount)
    {
        if (CurrentHp <= 0f) return;

        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        RaiseHpChanged();
    }

    public void UseStamina(float amount)
    {
        CurrentStamina = Mathf.Max(0f, CurrentStamina - amount);
        OnStaminaChanged?.Invoke(CurrentStamina, _maxStamina);
    }

    public void RestoreStamina(float amount)
    {
        CurrentStamina = Mathf.Min(_maxStamina, CurrentStamina + amount);
        OnStaminaChanged?.Invoke(CurrentStamina, _maxStamina);
    }
}
