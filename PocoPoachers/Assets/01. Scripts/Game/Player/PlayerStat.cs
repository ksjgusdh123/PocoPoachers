using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStat : StatBase
{
    [SerializeField] private float _maxHp = 100f;
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _staminaRegenRate = 15f;
    [SerializeField] private float _staminaRegenDelay = 1f;
    [SerializeField] private float _sprintStaminaDrain = 10f;  // 달리기 초당 소모량
    [SerializeField] private float _dodgeStaminaCost = 20f;    // 구르기 1회 소모량

    [Header("이동")]
    [SerializeField] private float _moveSpeed = 5f;    // 걷기 기준 속도
    [SerializeField] private float _sprintSpeed = 8f;  // 달리기 기준 속도

    [Header("배터리")]
    [SerializeField] private float _maxBattery = 100f;
    [SerializeField] private float _reduceBatteryRate = 1f;  // 초당 감소량

    public float MaxStamina => _maxStamina + _enhancementMaxStaminaBonus;
    public float CurrentStamina { get; private set; }

    // 방어구 등으로 인한 이동속도 배율 (내부에서만 관리)
    private float _armorMoveSpeedMultiplier = 1f;

    private Inventory _inventory;
    private const float OverweightMoveSpeedMultiplier = 0.2f; // 무게 초과 시 이동속도 80% 감소
    private const float DownedMoveSpeedMultiplier = 0.1f;     // 구조대기(사망) 상태 시 이동속도 90% 감소

    // 인벤토리 무게가 최대치를 초과한 상태인지
    private bool IsOverweight => _inventory != null && _inventory.MaxWeight > 0 && _inventory.CurrentWeight > _inventory.MaxWeight;

    // 사망 후 구조대기 상태 시 적용되는 이동속도 배율
    private float DownedMultiplier => IsDead ? DownedMoveSpeedMultiplier : 1f;

    // 배율 미적용 기준 속도 (애니메이션 정규화용)
    public float BaseMoveSpeed => _moveSpeed + _enhancementMoveSpeedBonus;

    // 배율이 모두 적용된 최종 이동/달리기 속도
    public float MoveSpeed => (_moveSpeed + _enhancementMoveSpeedBonus) * _armorMoveSpeedMultiplier * (IsOverweight ? OverweightMoveSpeedMultiplier : 1f) * DownedMultiplier;
    public float SprintSpeed => (_sprintSpeed + _enhancementMoveSpeedBonus) * _armorMoveSpeedMultiplier * (IsOverweight ? OverweightMoveSpeedMultiplier : 1f) * DownedMultiplier;

    protected override float DefenseRate => base.DefenseRate;

    public float MaxBattery => _maxBattery + _enhancementMaxBatteryBonus;
    public float CurrentBattery { get; private set; }

    public event Action<float, float> OnStaminaChanged;
    public event Action<float, float> OnBatteryChanged;

    private float _lastStaminaUseTime = float.NegativeInfinity;
    private float _vitalSyncTimer;
    private const float VitalSyncInterval = 2f;
    private float _totalMaxHpBonus;
    private float _enhancementMaxHpBonus;
    private float _enhancementMaxBatteryBonus;
    private float _enhancementMaxStaminaBonus;
    private float _enhancementMoveSpeedBonus;

    // 쉘터(전투 없는 씬)에서는 배터리가 닳지 않는다 — 이동해 온 씬으로 판별해 스폰 시 1회 캐싱.
    private bool _isShelter;

    protected override void Awake()
    {
        base.Awake();
        _isShelter = SceneName.IsShelter(SceneManager.GetActiveScene().name);
        _inventory = GetComponent<Inventory>();
        MaxHp = GetCalculatedMaxHp();
        CurrentHp = MaxHp;
        CurrentStamina = MaxStamina;
        CurrentBattery = MaxBattery;
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
        if (IsOverweight) return;
        if (CurrentStamina >= MaxStamina) return;
        if (Time.time < _lastStaminaUseTime + _staminaRegenDelay) return;

        CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + _staminaRegenRate * Time.deltaTime);
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
    }

    private void DrainBattery()
    {
        if (_isShelter) return;
        if (CurrentBattery <= 0f) return;

        CurrentBattery = Mathf.Max(0f, CurrentBattery - _reduceBatteryRate * Time.deltaTime);
        OnBatteryChanged?.Invoke(CurrentBattery, MaxBattery);

        if (CurrentBattery <= 0f)
            Die();  // 배터리 방전 = 사망
    }

    public void ChargeBattery(float amount)
    {
        CurrentBattery = Mathf.Min(MaxBattery, CurrentBattery + amount);
        OnBatteryChanged?.Invoke(CurrentBattery, MaxBattery);
    }

    public void RestoreStamina(float amount)
    {
        CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + amount);
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
    }

    // 구르기 시도 — 스태미나가 충분하면 소모하고 true, 부족하면 false
    public bool TryUseDodgeStamina() => UseStamina(_dodgeStaminaCost);

    // 소모 성공 여부를 반환 (부족하면 false)
    private bool UseStamina(float amount)
    {
        if (CurrentStamina < amount) return false;

        CurrentStamina = Mathf.Max(0f, CurrentStamina - amount);
        _lastStaminaUseTime = Time.time;
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
        return true;
    }

    // 달리기 중 매 프레임 호출 — 초당 _sprintStaminaDrain 만큼 소모
    public void DrainSprintStamina()
    {
        DrainStamina(_sprintStaminaDrain * Time.deltaTime);
    }

    // 매 프레임 소모용 — 가능한 만큼만 소모
    private void DrainStamina(float amount)
    {
        CurrentStamina = Mathf.Max(0f, CurrentStamina - amount);
        _lastStaminaUseTime = Time.time;
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
    }

    public override void ApplyArmorStat(ArmorStatData data)
    {
        base.ApplyArmorStat(data);
        _totalMaxHpBonus += data.MaxHpBonus;
        _armorMoveSpeedMultiplier *= data.MoveSpeedMultiplier;

        MaxHp = GetCalculatedMaxHp();
        CurrentHp = Mathf.Min(CurrentHp + data.MaxHpBonus, MaxHp);
        RaiseHpChanged();
        OnLocalHpChanged(CurrentHp, MaxHp);
    }

    public override void RemoveArmorStat(ArmorStatData data)
    {
        base.RemoveArmorStat(data);
        _totalMaxHpBonus = Mathf.Max(0f, _totalMaxHpBonus - data.MaxHpBonus);
        _armorMoveSpeedMultiplier = data.MoveSpeedMultiplier > 0f
            ? _armorMoveSpeedMultiplier / data.MoveSpeedMultiplier
            : 1f;

        MaxHp = GetCalculatedMaxHp();
        CurrentHp = Mathf.Min(CurrentHp, MaxHp);
        RaiseHpChanged();
        OnLocalHpChanged(CurrentHp, MaxHp);
    }

    public void ApplyEnhancementStats(float maxHpBonus, float maxBatteryBonus, float maxStaminaBonus, float moveSpeedBonus)
    {
        float previousMaxHp = MaxHp;
        float previousMaxBattery = MaxBattery;
        float previousMaxStamina = MaxStamina;

        _enhancementMaxHpBonus = Mathf.Max(0f, maxHpBonus);
        _enhancementMaxBatteryBonus = Mathf.Max(0f, maxBatteryBonus);
        _enhancementMaxStaminaBonus = Mathf.Max(0f, maxStaminaBonus);
        _enhancementMoveSpeedBonus = Mathf.Max(0f, moveSpeedBonus);

        MaxHp = GetCalculatedMaxHp();
        CurrentHp = Mathf.Clamp(CurrentHp + Mathf.Max(0f, MaxHp - previousMaxHp), 0f, MaxHp);
        RaiseHpChanged();

        CurrentBattery = Mathf.Clamp(CurrentBattery + Mathf.Max(0f, MaxBattery - previousMaxBattery), 0f, MaxBattery);
        OnBatteryChanged?.Invoke(CurrentBattery, MaxBattery);

        CurrentStamina = Mathf.Clamp(CurrentStamina + Mathf.Max(0f, MaxStamina - previousMaxStamina), 0f, MaxStamina);
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);

        OnLocalHpChanged(CurrentHp, MaxHp);
    }

    private float GetCalculatedMaxHp()
    {
        return _maxHp + _totalMaxHpBonus + _enhancementMaxHpBonus;
    }

    // 맵 전환 시 저장된 활력치를 복원한다.
    // 방어구/강화의 Start()가 MaxHp를 확정한 뒤(다음 프레임) 적용해야 저장값에 보너스가 덧붙지 않는다.
    public void RestoreVitals(float hp, float stamina, float battery)
    {
        StartCoroutine(RestoreVitalsRoutine(hp, stamina, battery));
    }

    private IEnumerator RestoreVitalsRoutine(float hp, float stamina, float battery)
    {
        yield return null; // 모든 Start()(방어구/강화 적용) 이후로 미룬다

        CurrentHp = Mathf.Clamp(hp, 0f, MaxHp);
        CurrentStamina = Mathf.Clamp(stamina, 0f, MaxStamina);
        CurrentBattery = Mathf.Clamp(battery, 0f, MaxBattery);

        RaiseHpChanged();
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
        OnBatteryChanged?.Invoke(CurrentBattery, MaxBattery);
        OnLocalHpChanged(CurrentHp, MaxHp);
    }
}
