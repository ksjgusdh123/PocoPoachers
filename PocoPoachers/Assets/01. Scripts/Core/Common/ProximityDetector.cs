using System;
using UnityEngine;

public class ProximityDetector : MonoBehaviour
{
    [SerializeField] private LayerMask _targetLayer;

    public event Action OnEnter;
    public event Action OnExit;

    private void OnTriggerEnter(Collider other)
    {
        if ((_targetLayer & (1 << other.gameObject.layer)) == 0) return;
        OnEnter?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if ((_targetLayer & (1 << other.gameObject.layer)) == 0) return;
        OnExit?.Invoke();
    }
}
