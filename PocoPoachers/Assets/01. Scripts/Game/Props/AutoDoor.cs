using UnityEngine;

// frame_door_1/2/3 등 character_nearby(bool) 파라미터로 열림/닫힘을 전환하는 Animator Controller를 쓰는 자동문 공용 스크립트
// 트리거 콜라이더가 붙은 자식 오브젝트에 ProximityDetector를 두고 이 컴포넌트에 연결한다 (RescueInteractable과 동일 패턴)
[RequireComponent(typeof(Animator))]
public class AutoDoor : MonoBehaviour
{
    private static readonly int CharacterNearby = Animator.StringToHash("character_nearby");

    [SerializeField] private ProximityDetector _proximityDetector;

    [Header("사운드")]
    [SerializeField, Tooltip("sound.csv의 키. 비워두거나 테이블에 없으면 소리를 내지 않는다.")]
    private string _openSoundKey = "sfx_door_open";

    [SerializeField] private string _closeSoundKey = "sfx_door_close";

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();

        if (_proximityDetector != null)
        {
            _proximityDetector.OnEnter += OnPlayerEntered;
            _proximityDetector.OnExit += OnPlayerExited;
        }
    }

    private void OnDestroy()
    {
        if (_proximityDetector != null)
        {
            _proximityDetector.OnEnter -= OnPlayerEntered;
            _proximityDetector.OnExit -= OnPlayerExited;
        }
    }

    private void OnPlayerEntered()
    {
        _animator.SetBool(CharacterNearby, true);
        PlayDoorSfx(_openSoundKey);
    }

    private void OnPlayerExited()
    {
        _animator.SetBool(CharacterNearby, false);
        PlayDoorSfx(_closeSoundKey);
    }

    // 문은 맵 곳곳에 있어 2D로 내면 반대편 문 소리까지 귀 옆에서 울린다 — 총성과 같이 위치 기반으로 낸다.
    private void PlayDoorSfx(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        SoundManager.GetInstance()?.PlaySfxAt(key, transform.position);
    }
}
