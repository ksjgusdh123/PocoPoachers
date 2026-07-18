using System;
using UnityEngine;

public class Generator : MonoBehaviour, IInteractable
{
    [SerializeField] private float _maxPowerCapacity = 3600f;
    [SerializeField] private float _currentPower = 1800f;
    [SerializeField] private float _baseGridDrainRate = 1f;
    [SerializeField] private float _crankChargeAmount = 2f;
    [SerializeField] private float _crankMaxCap = 100f;

    public static Generator Instance { get; private set; }

    public event Action<float, float> OnPowerChanged; // (current, max)

    public float CurrentPower => _currentPower;
    public float MaxPowerCapacity => _maxPowerCapacity;
    public bool HasPower => _currentPower > 0f; // MinPowerThreshold = 0

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (_currentPower <= 0f) return;

        _currentPower = Mathf.Max(0f, _currentPower - _baseGridDrainRate * Time.deltaTime);
        OnPowerChanged?.Invoke(_currentPower, _maxPowerCapacity);
    }

    // 시설의 즉시차감(건당) 비용 — 충당 가능할 때만 실행
    public bool TryConsume(float amount)
    {
        if (_currentPower < amount) return false;

        _currentPower -= amount;
        OnPowerChanged?.Invoke(_currentPower, _maxPowerCapacity);
        return true;
    }

    public bool TryInsertFuel(ItemData fuelItem, int amount)
    {
        if (fuelItem == null || amount <= 0) return false;

        var fuel = GeneratorFuelTable.Instance.Get(fuelItem.id);
        if (fuel == null) return false;

        _currentPower = Mathf.Min(_maxPowerCapacity, _currentPower + fuel.power_seconds * amount);
        OnPowerChanged?.Invoke(_currentPower, _maxPowerCapacity);
        return true;
    }

    // 최후의 수단: 연료 없이도 소량 강제 충전, 100.0 상한
    public void CrankCharge()
    {
        if (_currentPower >= _crankMaxCap) return;

        _currentPower = Mathf.Min(_currentPower + _crankChargeAmount, _crankMaxCap);
        OnPowerChanged?.Invoke(_currentPower, _maxPowerCapacity);
    }

    public void OnInteract(PlayerController player)
    {
        player.SetInventoryOpen(true);
        UIManager.GetInstance().Show(UIType.Generator);

        var ui = player.GetGeneratorUI;
        ui?.SendMessage("Open", player, SendMessageOptions.DontRequireReceiver);

        player.SwitchInputMap(PlayerInputMapType.ItemBox);
    }

    public void OnInteractExit(PlayerController player)
    {
        UIManager.GetInstance().Hide(UIType.Generator);
        player.SetInventoryOpen(false);

        player.SwitchInputMap(PlayerInputMapType.Game);
        UIManager.GetInstance().ChangeMouseCursor(true);
    }
}
