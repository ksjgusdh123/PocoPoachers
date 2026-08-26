using UnityEngine;

// 애니메이션 이벤트로 호출되는 캐릭터 사운드. 플레이어와 적이 함께 쓴다.
// 유니티는 Animator와 같은 GameObject에 붙은 컴포넌트에서만 이벤트 함수를 찾으므로,
// 이 스크립트는 Animator와 같은 오브젝트에 있어야 한다 (플레이어는 모델 자식, 적은 루트).
public class CharacterAudio : MonoBehaviour
{
    [SerializeField] private float _footstepAudibleRange = 25f;
    // sound.csv에 {키}_1 ~ {키}_N으로 등록된 변형 중 매번 하나를 무작위로 고른다.
    // 같은 파일만 반복하면 몇 걸음 만에 티가 난다.
    [SerializeField] private int _footstepVariationCount = 5;
    // 대각선 이동은 블렌드 트리에서 두 클립이 동시에 재생돼 한 걸음에 이벤트가 두 번 뜬다.
    // 상태 전환 중에도 마찬가지라, 실제 걸음 간격보다 짧은 재호출은 버린다.
    [SerializeField] private float _minFootstepInterval = 0.12f;

    // 내 발소리는 2D로 나가 감쇠가 없어서 팀원/적 발소리보다 훨씬 크게 들린다. 그만큼 줄인다.
    [SerializeField, Range(0f, 1f)] private float _localFootstepVolume = 0.5f;

    private bool _isLocalPlayer;
    private float _lastFootstepTime = float.NegativeInfinity;

    private void Awake()
    {
        _isLocalPlayer = GetComponentInParent<PlayerController>() != null;
    }

    // 애니메이션 이벤트에서 호출 — String 칸에 키 접두사를 넣는다 (예: sfx_footstep_dirt)
    public void OnFootstep(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (Time.time - _lastFootstepTime < _minFootstepInterval) return;

        _lastFootstepTime = Time.time;

        string variation = _footstepVariationCount > 1
            ? $"{key}_{Random.Range(1, _footstepVariationCount + 1)}"
            : key;

        Play(variation);
    }

    // 내 소리는 2D로 또렷하게 — 리스너가 카메라라 3D로 재생하면 12m 밖으로 계산돼 묻힌다
    private void Play(string key)
    {
        var sound = SoundManager.GetInstance();
        if (sound == null) return;

        if (_isLocalPlayer)
            sound.PlaySfx(key, _localFootstepVolume);
        else
            sound.PlaySfxAt(key, transform.position, _footstepAudibleRange);
    }
}
