using System;
using UnityEngine;

public class PlayerStat : StatBase
{
    [SerializeField] private float _maxHp = 100f;
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _staminaRegenRate = 15f;
    [SerializeField] private float _staminaRegenDelay = 1f;

    public float MaxStamina => _maxStamina;
    public float CurrentStamina { get; private set; }

    public event Action<float, float> OnStaminaChanged;

    private float _lastStaminaUseTime = float.NegativeInfinity;

    protected override void Awake()
    {
        base.Awake();
        MaxHp = _maxHp;
        CurrentHp = _maxHp;
        CurrentStamina = _maxStamina;
    }

    private void Update()
    {
        if (CurrentStamina >= _maxStamina) return;
        if (Time.time < _lastStaminaUseTime + _staminaRegenDelay) return;

        CurrentStamina = Mathf.Min(_maxStamina, CurrentStamina + _staminaRegenRate * Time.deltaTime);
        OnStaminaChanged?.Invoke(CurrentStamina, _maxStamina);

        Debug.Log($"현재 스테미나 : {CurrentStamina}");
    }

    public void Heal(float amount)
    {
        if (CurrentHp <= 0f) return;

        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        RaiseHpChanged();
    }

    // 소모 성공 여부를 반환 (부족하면 false)
    public bool UseStamina(float amount)
    {
        if (CurrentStamina < amount) return false;

        CurrentStamina = Mathf.Max(0f, CurrentStamina - amount);
        _lastStaminaUseTime = Time.time;
        OnStaminaChanged?.Invoke(CurrentStamina, _maxStamina);
        return true;
    }

    // 매 프레임 소모용 (달리기) — 가능한 만큼만 소모하고 남은 양 반환
    public void DrainStamina(float amount)
    {
        CurrentStamina = Mathf.Max(0f, CurrentStamina - amount);
        _lastStaminaUseTime = Time.time;
        OnStaminaChanged?.Invoke(CurrentStamina, _maxStamina);
    }
}
