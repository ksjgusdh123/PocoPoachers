using System;
using System.Collections;
using UnityEngine;

public class ChargingStation : MonoBehaviour, IInteractable
{
    [SerializeField] private float _chargeDuration = 3f;

    private Coroutine _chargeCoroutine;

    public static event Action<float> OnChargeStarted;
    public static event Action OnChargeEnded;

    public void OnInteract(PlayerController player)
    {
        if (_chargeCoroutine != null) return;
        OnChargeStarted?.Invoke(_chargeDuration);
        _chargeCoroutine = StartCoroutine(ChargeRoutine(player));
    }

    private IEnumerator ChargeRoutine(PlayerController player)
    {
        yield return new WaitForSeconds(_chargeDuration);

        _chargeCoroutine = null;
        OnChargeEnded?.Invoke();

        var stat = player.GetComponent<PlayerStat>();
        stat.ChargeBattery(stat.MaxBattery);

        player.EndInteraction(this);
    }

    public void OnInteractExit(PlayerController player)
    {
        if (_chargeCoroutine == null) return;
        StopCoroutine(_chargeCoroutine);
        _chargeCoroutine = null;
        OnChargeEnded?.Invoke();
    }
}
