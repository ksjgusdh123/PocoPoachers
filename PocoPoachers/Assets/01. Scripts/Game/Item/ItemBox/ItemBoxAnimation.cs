using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ItemBoxAnimation : MonoBehaviour
{
    [Header("뚜껑 회전 방식 (비워두면 기존 Animator 사용)")]
    [SerializeField] private Transform _lid;
    [Tooltip("모델 프리팹의 뚜껑 경로. 직접 참조가 없을 때만 사용한다.")]
    [SerializeField] private string _lidPath;
    [SerializeField] private Vector3 _openRotationOffset = new Vector3(-105f, 0f, 0f);
    [SerializeField, Min(0.01f)] private float _openDuration = 0.4f;
    [SerializeField, Min(0.01f)] private float _closeDuration = 0.3f;

    private Animator _animator;
    private Quaternion _closedRotation;
    private Coroutine _animation;
    private bool _initialized;
    private bool _isOpen;
    private static readonly int IsOpenHash = Animator.StringToHash("IsOpen");

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;
        if (_lid == null && !string.IsNullOrEmpty(_lidPath))
            _lid = transform.Find(_lidPath);
        if (_lid != null) _closedRotation = _lid.localRotation;
        _animator = GetComponent<Animator>();
    }

    public void SetOpen(bool isOpen)
    {
        EnsureInitialized();
        _isOpen = isOpen;
        if (_lid == null)
        {
            if (_animator != null) _animator.SetBool(IsOpenHash, isOpen);
            return;
        }

        if (_animation != null) StopCoroutine(_animation);
        Quaternion target = GetTargetRotation();
        if (!isActiveAndEnabled)
        {
            _lid.localRotation = target;
            _animation = null;
            return;
        }
        _animation = StartCoroutine(AnimateLid(target, isOpen ? _openDuration : _closeDuration));
    }

    private Quaternion GetTargetRotation() =>
        _isOpen ? _closedRotation * Quaternion.Euler(_openRotationOffset) : _closedRotation;

    private IEnumerator AnimateLid(Quaternion target, float duration)
    {
        // 도중에 반대 명령을 받아도 현재 각도에서 이어서 움직인다.
        Quaternion start = _lid.localRotation;
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            _lid.localRotation = Quaternion.Slerp(start, target, t);
            yield return null;
        }
        _lid.localRotation = target;
        _animation = null;
    }

    private void OnDisable()
    {
        if (_animation != null) StopCoroutine(_animation);
        _animation = null;
        if (_initialized && _lid != null) _lid.localRotation = GetTargetRotation();
    }

    [ContextMenu("열기 테스트 (플레이 모드)")]
    private void TestOpen() { if (Application.isPlaying) SetOpen(true); }

    [ContextMenu("닫기 테스트 (플레이 모드)")]
    private void TestClose() { if (Application.isPlaying) SetOpen(false); }
}
