using UnityEngine;

// frame_door_1/2/3 등 character_nearby(bool) 파라미터로 열림/닫힘을 전환하는 Animator Controller를 쓰는 자동문 공용 스크립트
// 트리거 콜라이더가 붙은 자식 오브젝트에 ProximityDetector를 두고 이 컴포넌트에 연결한다 (RescueInteractable과 동일 패턴)
[RequireComponent(typeof(Animator))]
public class AutoDoor : MonoBehaviour
{
    private static readonly int CharacterNearby = Animator.StringToHash("character_nearby");

    [SerializeField] private ProximityDetector _proximityDetector;

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

    private void OnPlayerEntered() => _animator.SetBool(CharacterNearby, true);

    private void OnPlayerExited() => _animator.SetBool(CharacterNearby, false);
}
