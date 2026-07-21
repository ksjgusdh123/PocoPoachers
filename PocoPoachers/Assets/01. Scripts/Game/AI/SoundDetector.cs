using UnityEngine;

public class SoundDetector : MonoBehaviour
{
    private TargetDetector _targetDetector;

    private void Awake()
    {
        _targetDetector = GetComponent<TargetDetector>();
    }

    private void OnEnable()
    {
        SoundEvent.OnSoundEmitted += OnSoundHeard;
    }

    private void OnDisable()
    {
        SoundEvent.OnSoundEmitted -= OnSoundHeard;
    }

    private void OnSoundHeard(Vector3 soundPosition, float soundRange, GameObject source)
    {
        if (source == gameObject) return;
        if (_targetDetector == null)
        {
            Debug.LogWarning($"[Sound] {name}: TargetDetector 없음 — 소리 무시", this);
            return;
        }

        // 거리 비교는 모두 제곱거리로 처리해 sqrt를 피한다 (총성/발소리는 이동 내내 자주 들어옴)
        float sqrToSound = (transform.position - soundPosition).sqrMagnitude;
        if (sqrToSound > soundRange * soundRange)
        {
            return; // 소음 반경 밖이면 무시
        }

        // 이미 타깃이 있으면, 이번 소음원이 더 가까울 때만 교체한다
        var current = _targetDetector.CurrentTarget;
        if (current != null)
        {
            float sqrToCurrent = (transform.position - current.transform.position).sqrMagnitude;
            if (sqrToSound >= sqrToCurrent)
            {
                Debug.Log($"[Sound] {name}: '{source.name}'(dist={Mathf.Sqrt(sqrToSound):F1}) 유지 — 기존 타깃 '{current.name}'(dist={Mathf.Sqrt(sqrToCurrent):F1})이 더 가까움");
                return; // 기존 타깃이 더 가깝거나 같으면 유지
            }
        }

        Debug.Log($"[Sound] {name}: '{source.name}'로 타깃 교체 (dist={Mathf.Sqrt(sqrToSound):F1})");
        _targetDetector.ForceSetTarget(source);
    }
}
