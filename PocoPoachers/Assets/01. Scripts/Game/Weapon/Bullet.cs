using System;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private float _speed;
    private float _damage;
    private float _range;
    private float _traveledDistance;
    private Vector3 _direction;
    private Action _onRelease;
    private bool _applyDamage = true;
    private TrailRenderer _trail;

    private void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
    }

    public void Initialize(float speed, float damage, float range, Vector3 direction, Action onRelease, bool applyDamage = true)
    {
        _speed = speed;
        _damage = damage;
        _range = range;
        _direction = direction;
        _onRelease = onRelease;
        _applyDamage = applyDamage;
        _traveledDistance = 0f;
    }

    private void Update()
    {
        float step = _speed * Time.deltaTime;
        transform.Translate(_direction * step, Space.World);
        _traveledDistance += step;

        if (_traveledDistance >= _range)
            Release();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_applyDamage && other.TryGetComponent<IDamageable>(out var damageable))
            damageable.TakeDamage(_damage);

        Release();
    }

    private void Release()
    {
        _trail?.Clear();
        _onRelease?.Invoke();
    }
}
