using System.Collections;
using UnityEngine;

// 체크포인트 부활 — 부활 지점이 등록돼 있으면 사망 시 포드 호송/관전 대신
// 마지막 지점에서 다시 시작한다. 튜토리얼에서 진행을 잃지 않게 하는 용도.
//
// 지점을 아무도 등록하지 않은 씬(레이드/쉘터)에서는 HasPoint가 false라
// PlayerController가 기존 사망 흐름을 그대로 탄다 — 이 컴포넌트가 붙어 있어도 안전하다.
// 지점 등록은 TutorialWaypoint가 도착할 때마다 해준다.
public class PlayerRespawnPoint : MonoBehaviour
{
    [Tooltip("첫 부활 지점. 비워두면 체크포인트를 하나라도 밟기 전까지는 기존 사망 처리로 넘어간다")]
    [SerializeField] private Transform _initialPoint;

    [Tooltip("부활 시 회복할 비율 (1이면 최대치)")]
    [SerializeField, Range(0.1f, 1f)] private float _reviveRatio = 1f;

    [Tooltip("쓰러진 뒤 부활까지의 대기 시간(초)")]
    [SerializeField] private float _respawnDelay = 0.5f;

    public bool HasPoint { get; private set; }

    private Vector3 _position;
    private Quaternion _rotation;
    private bool _respawning;

    private CharacterController _characterController;
    private PlayerStat _stat;
    private PlayerInputHandler _inputHandler;
    private PlayerMovement _movement;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _stat = GetComponent<PlayerStat>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        _movement = GetComponent<PlayerMovement>();

        if (_initialPoint != null)
            SetPoint(_initialPoint.position, _initialPoint.rotation);
    }

    public void SetPoint(Vector3 position) => SetPoint(position, transform.rotation);

    public void SetPoint(Vector3 position, Quaternion rotation)
    {
        _position = position;
        _rotation = rotation;
        HasPoint = true;
    }

    public void Respawn()
    {
        if (!HasPoint || _respawning) return;

        _respawning = true;
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        // 쓰러진 동안 움직이거나 쏘지 못하게 전투 입력이 없는 맵으로 돌린다
        // (PlayerController.PlayEscapeBeam이 호송 연출 중에 쓰는 것과 같은 방식)
        _inputHandler?.SwitchInputActionMap(PlayerInputMapType.Inventory);

        // 부활 지점에 먼저 이펙트를 띄워서 어디로 돌아가는지 보이게 한다.
        // 임시로 적 사망 VFX를 빌려 쓰는 중 — 전용 이펙트가 나오면 교체할 것.
        DeathVFXPool.Instance?.Spawn(_position);

        // 쓰러지는 순간 바로 옮기면 툭 끊겨서, 잠깐 두고 부활시킨다
        if (_respawnDelay > 0f) yield return new WaitForSeconds(_respawnDelay);

        DoRespawn();

        // 다음 프레임으로 미뤄야 이번 입력의 뗌(release)을 새 맵이 놓치지 않는다
        _inputHandler?.SwitchToGameplayMapNextFrame();

        _respawning = false;
    }

    private void DoRespawn()
    {
        // 부활보다 이동이 먼저다 — Revive가 쏘는 OnRevive를 PlayerController가 받아
        // 관전 해제와 카메라 복귀를 하는데, 그 시점엔 위치가 이미 맞아야 한다
        Teleport();

        // 쓰러짐 자세는 옮겨지는 순간에 푼다. Revive의 OnRevive로도 풀리지만,
        // 구독 순서에 맡기지 않고 위치와 같은 타이밍에 맞춘다
        _movement?.SetDown(false);

        if (_stat == null) return;

        float hp = _stat.MaxHp * _reviveRatio;
        _stat.Revive(hp);

        // 배터리가 0인 채로 되살리면 다음 프레임에 방전으로 또 죽는다
        _stat.RestoreVitals(hp, _stat.MaxStamina, _stat.MaxBattery);
    }

    // CharacterController가 켜져 있는 동안 위치를 바꾸면 컨트롤러가 원래 자리로 되돌린다
    private void Teleport()
    {
        if (_characterController != null) _characterController.enabled = false;

        transform.SetPositionAndRotation(_position, _rotation);

        if (_characterController != null) _characterController.enabled = true;
    }
}
