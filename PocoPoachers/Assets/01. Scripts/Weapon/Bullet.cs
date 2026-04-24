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

    public void Initialize(float speed, float damage, float range, Vector3 direction, Action onRelease)
    {
        _speed = speed;
        _damage = damage;
        _range = range;
        _direction = direction;
        _onRelease = onRelease;
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
        if (other.TryGetComponent<IDamageable>(out var damageable))
            damageable.TakeDamage(_damage);

        Release();
    }

    private void Release()
    {
        _onRelease?.Invoke();
    }
}
