using UnityEngine;
using UnityEngine.Pool;

public class ShellCasingPool : Singleton<ShellCasingPool>
{
    [SerializeField] private GameObject _shellPrefab;

    [Header("배출 설정")]
    [SerializeField] private float _ejectSideForce = 3f;
    [SerializeField] private float _ejectUpForce = 2f;
    [SerializeField] private float _ejectRandomness = 0.3f;
    [SerializeField] private float _ejectTorque = 15f;
    [SerializeField] private float _lifetime = 3f;

    [Header("풀 설정")]
    [SerializeField] private int _defaultCapacity = 20;
    [SerializeField] private int _maxSize = 50;

    private ObjectPool<ShellCasing> _pool;

    protected override void Awake()
    {
        base.Awake();
        _pool = new ObjectPool<ShellCasing>(
            createFunc: () => Instantiate(_shellPrefab).GetComponent<ShellCasing>(),
            actionOnGet: s => s.gameObject.SetActive(true),
            actionOnRelease: s => s.gameObject.SetActive(false),
            actionOnDestroy: s => Destroy(s.gameObject),
            defaultCapacity: _defaultCapacity,
            maxSize: _maxSize
        );
    }

    public void Eject(Transform ejectPort)
    {
        if (_shellPrefab == null || ejectPort == null) return;

        ShellCasing shell = _pool.Get();
        shell.transform.SetPositionAndRotation(ejectPort.position, ejectPort.rotation);

        Vector3 force = ejectPort.right * _ejectSideForce
                      + ejectPort.up * _ejectUpForce
                      + Random.insideUnitSphere * _ejectRandomness;
        Vector3 torque = Random.insideUnitSphere * _ejectTorque;

        shell.Eject(force, torque, _lifetime);
    }

    public void Release(ShellCasing shell)
    {
        _pool.Release(shell);
    }
}
