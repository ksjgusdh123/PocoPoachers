using System;
using UnityEngine;

public class PlayerStat : StatBase
{
    [SerializeField] private float _maxHp = 100f;
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _staminaRegenRate = 15f;
    [SerializeField] private float _staminaRegenDelay = 1f;

    [Header("배고픔/목마름")]
    [SerializeField] private float _maxHunger = 100f;
    [SerializeField] private float _maxThirst = 100f;
    [SerializeField] private float _hungerDrainRate = 1f;  // 초당 감소량
    [SerializeField] private float _thirstDrainRate = 2f;  // 초당 감소량 (목마름이 더 빠름)

    public float MaxStamina => _maxStamina;
    public float CurrentStamina { get; private set; }
    public float ArmorMoveSpeedMultiplier { get; private set; } = 1f;

    protected override float Defense => _totalDefense;

    public float MaxHunger => _maxHunger;
    public float CurrentHunger { get; private set; }

    public float MaxThirst => _maxThirst;
    public float CurrentThirst { get; private set; }

    public event Action<float, float> OnStaminaChanged;
    public event Action<float, float> OnHungerChanged;
    public event Action<float, float> OnThirstChanged;

    private float _lastStaminaUseTime = float.NegativeInfinity;
    private float _totalDefense;
    private float _totalMaxHpBonus;

    protected override void Awake()
    {
        base.Awake();
        MaxHp = _maxHp;
        CurrentHp = _maxHp;
        CurrentStamina = _maxStamina;
        CurrentHunger = _maxHunger;
        CurrentThirst = _maxThirst;
        ItemUseSystem.Register(this);
    }

    private void Start()
    {
        FindAnyObjectByType<HpUI>(FindObjectsInactive.Include)?.Setup(this);

        foreach (var ui in FindObjectsByType<VitalUI>(FindObjectsInactive.Include))
            ui.Setup(this);

        FindAnyObjectByType<StaminaWorldUI>(FindObjectsInactive.Include)?.Setup(this, transform);
    }

    private void Update()
    {
        RegenerateStamina();
        DrainHungerAndThirst();
    }

    private void RegenerateStamina()
    {
        if (CurrentStamina >= _maxStamina) return;
        if (Time.time < _lastStaminaUseTime + _staminaRegenDelay) return;

        CurrentStamina = Mathf.Min(_maxStamina, CurrentStamina + _staminaRegenRate * Time.deltaTime);
        OnStaminaChanged?.Invoke(CurrentStamina, _maxStamina);
    }

    private void DrainHungerAndThirst()
    {
        if (CurrentHunger > 0f)
        {
            CurrentHunger = Mathf.Max(0f, CurrentHunger - _hungerDrainRate * Time.deltaTime);
            OnHungerChanged?.Invoke(CurrentHunger, _maxHunger);
        }

        if (CurrentThirst > 0f)
        {
            CurrentThirst = Mathf.Max(0f, CurrentThirst - _thirstDrainRate * Time.deltaTime);
            OnThirstChanged?.Invoke(CurrentThirst, _maxThirst);
        }
    }

    public void Heal(float amount)
    {
        if (CurrentHp <= 0f) return;

        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        RaiseHpChanged();
    }

    public void EatFood(float amount)
    {
        CurrentHunger = Mathf.Min(_maxHunger, CurrentHunger + amount);
        OnHungerChanged?.Invoke(CurrentHunger, _maxHunger);
    }

    public void DrinkWater(float amount)
    {
        CurrentThirst = Mathf.Min(_maxThirst, CurrentThirst + amount);
        OnThirstChanged?.Invoke(CurrentThirst, _maxThirst);
    }

    public void RestoreStamina(float amount)
    {
        CurrentStamina = Mathf.Min(_maxStamina, CurrentStamina + amount);
        OnStaminaChanged?.Invoke(CurrentStamina, _maxStamina);
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

    public void ApplyArmorStat(ArmorData data)
    {
        _totalDefense += data.defense;
        _totalMaxHpBonus += data.maxHpBonus;
        ArmorMoveSpeedMultiplier *= data.moveSpeedMultiplier;

        MaxHp = _maxHp + _totalMaxHpBonus;
        CurrentHp = Mathf.Min(CurrentHp + data.maxHpBonus, MaxHp);
        RaiseHpChanged();
    }

    public void RemoveArmorStat(ArmorData data)
    {
        _totalDefense = Mathf.Max(0f, _totalDefense - data.defense);
        _totalMaxHpBonus = Mathf.Max(0f, _totalMaxHpBonus - data.maxHpBonus);
        ArmorMoveSpeedMultiplier = data.moveSpeedMultiplier > 0f
            ? ArmorMoveSpeedMultiplier / data.moveSpeedMultiplier
            : 1f;

        MaxHp = _maxHp + _totalMaxHpBonus;
        CurrentHp = Mathf.Min(CurrentHp, MaxHp);
        RaiseHpChanged();
    }
}
