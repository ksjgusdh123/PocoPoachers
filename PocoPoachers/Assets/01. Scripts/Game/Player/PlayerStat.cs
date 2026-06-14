using System;
using UnityEngine;

public class PlayerStat : StatBase
{
    [SerializeField] private float _maxHp = 100f;
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _staminaRegenRate = 15f;
    [SerializeField] private float _staminaRegenDelay = 1f;

    [Header("이동")]
    [SerializeField] private float _moveSpeed = 5f;    // 걷기 기준 속도
    [SerializeField] private float _sprintSpeed = 8f;  // 달리기 기준 속도

    [Header("배터리")]
    [SerializeField] private float _maxBattery = 100f;
    [SerializeField] private float _reduceBatteryRate = 1f;  // 초당 감소량

    public float MaxStamina => _maxStamina;
    public float CurrentStamina { get; private set; }

    // 방어구 등으로 인한 이동속도 배율 (내부에서만 관리)
    private float _armorMoveSpeedMultiplier = 1f;

    // 배율 미적용 기준 속도 (애니메이션 정규화용)
    public float BaseMoveSpeed => _moveSpeed;

    // 배율이 모두 적용된 최종 이동/달리기 속도
    public float MoveSpeed => _moveSpeed * _armorMoveSpeedMultiplier;
    public float SprintSpeed => _sprintSpeed * _armorMoveSpeedMultiplier;

    protected override float DefenseRate => base.DefenseRate;

    public float MaxBattery => _maxBattery;
    public float CurrentBattery { get; private set; }

    public event Action<float, float> OnStaminaChanged;
    public event Action<float, float> OnBatteryChanged;

    private float _lastStaminaUseTime = float.NegativeInfinity;
    private float _vitalSyncTimer;
    private const float VitalSyncInterval = 2f;
    private float _totalMaxHpBonus;

    protected override void Awake()
    {
        base.Awake();
        MaxHp = _maxHp;
        CurrentHp = _maxHp;
        CurrentStamina = _maxStamina;
        CurrentBattery = _maxBattery;
        ItemUseSystem.Register(this);
    }

    protected override void OnLocalHpChanged(float hp, float maxHp)
    {
        RoomSync.StatSync(hp, maxHp, CurrentStamina, CurrentBattery, DefenseRate);
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
        DrainBattery();

        _vitalSyncTimer -= Time.deltaTime;
        if (_vitalSyncTimer <= 0f)
        {
            _vitalSyncTimer = VitalSyncInterval;
            RoomSync.StatSync(CurrentHp, MaxHp, CurrentStamina, CurrentBattery, DefenseRate);
        }
    }

    private void RegenerateStamina()
    {
        if (CurrentStamina >= _maxStamina) return;
        if (Time.time < _lastStaminaUseTime + _staminaRegenDelay) return;

        CurrentStamina = Mathf.Min(_maxStamina, CurrentStamina + _staminaRegenRate * Time.deltaTime);
        OnStaminaChanged?.Invoke(CurrentStamina, _maxStamina);
    }

    private void DrainBattery()
    {
        if (CurrentBattery <= 0f) return;

        CurrentBattery = Mathf.Max(0f, CurrentBattery - _reduceBatteryRate * Time.deltaTime);
        OnBatteryChanged?.Invoke(CurrentBattery, _maxBattery);
    }

    public void Heal(float amount)
    {
        if (CurrentHp <= 0f) return;

        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        RaiseHpChanged();
        OnLocalHpChanged(CurrentHp, MaxHp);
    }

    public void ChargeBattery(float amount)
    {
        CurrentBattery = Mathf.Min(_maxBattery, CurrentBattery + amount);
        OnBatteryChanged?.Invoke(CurrentBattery, _maxBattery);
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

    public override void ApplyArmorStat(ArmorStatData data)
    {
        base.ApplyArmorStat(data);
        _totalMaxHpBonus += data.MaxHpBonus;
        _armorMoveSpeedMultiplier *= data.MoveSpeedMultiplier;

        MaxHp = _maxHp + _totalMaxHpBonus;
        CurrentHp = Mathf.Min(CurrentHp + data.MaxHpBonus, MaxHp);
        RaiseHpChanged();
    }

    public override void RemoveArmorStat(ArmorStatData data)
    {
        base.RemoveArmorStat(data);
        _totalMaxHpBonus = Mathf.Max(0f, _totalMaxHpBonus - data.MaxHpBonus);
        _armorMoveSpeedMultiplier = data.MoveSpeedMultiplier > 0f
            ? _armorMoveSpeedMultiplier / data.MoveSpeedMultiplier
            : 1f;

        MaxHp = _maxHp + _totalMaxHpBonus;
        CurrentHp = Mathf.Min(CurrentHp, MaxHp);
        RaiseHpChanged();
    }
}
