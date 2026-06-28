using UnityEngine;

// 피격 이벤트를 감지해서 "구르고 싶다" 신호만 BT에 제공. 실제 구르기 처리는 BT의 DodgeRollAction이 담당
[RequireComponent(typeof(EnemyStat))]
public class AIDodgeState : MonoBehaviour
{
    [SerializeField] private float _cooldown = 10f;

    public bool WantsToDodge { get; private set; }
    public GameObject LastAttacker { get; private set; }

    private EnemyStat _stat;
    private float _lastDodgeTime = float.NegativeInfinity;

    private void Awake()
    {
        _stat = GetComponent<EnemyStat>();
    }

    private void OnEnable()
    {
        _stat.OnDamaged += OnDamaged;
    }

    private void OnDisable()
    {
        _stat.OnDamaged -= OnDamaged;
    }

    private void OnDamaged(float damage, Vector3 pos, GameObject attacker)
    {
        if (Time.time < _lastDodgeTime + _cooldown) return;

        WantsToDodge = true;
        LastAttacker = attacker;
    }

    // DodgeRollAction이 구르기를 시작할 때 호출 — 신호 소비 + 쿨다운 갱신
    public void ConsumeDodge()
    {
        WantsToDodge = false;
        _lastDodgeTime = Time.time;
    }
}
