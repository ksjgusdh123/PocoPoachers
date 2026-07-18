using UnityEngine;

// 원격 플레이어(게스트 등)의 발소리를 호스트에서 방출한다.
// AI는 호스트에서만 구동되므로, 게스트의 발소리도 호스트가 대신 방출해야 호스트 AI가 인식한다.
// 이동은 이미 H_Move(위치 + IsSprinting)로 동기화되므로 추가 패킷은 없다 — 인터벌은 호스트 자체 타이머로 돈다.
// 로컬 플레이어(PlayerMovement.EmitFootstep)와 동일한 규칙을 대상만 원격 플레이어로 바꿔 적용한다.
[RequireComponent(typeof(WorldObject))]
public class RemoteFootstepEmitter : MonoBehaviour
{
    [SerializeField] private float _walkSoundRange = 4f;      // 걷기 시 발소리 인식 반경
    [SerializeField] private float _sprintSoundRange = 10f;   // 달리기 시 발소리 인식 반경
    [SerializeField] private float _walkStepInterval = 0.5f;  // 걷기 발소리 방출 간격(초)
    [SerializeField] private float _sprintStepInterval = 0.3f; // 달리기 발소리 방출 간격(초)
    [SerializeField] private float _moveThresholdSqr = 0.01f; // 이 값 이하의 속도는 정지로 간주

    private WorldObject _worldObject;
    private float _nextStepTime;

    private void Awake() => _worldObject = GetComponent<WorldObject>();

    private void Update()
    {
        if (!RoomManager.IsHost) return;                       // 방출은 AI를 구동하는 호스트에서만
        if (_worldObject.Kind != ObjectKind.Player) return;
        if (_worldObject.PlanarMoveSqr <= _moveThresholdSqr) return; // 정지 중이면 발소리 없음

        if (Time.time < _nextStepTime) return;

        bool sprinting = _worldObject.IsSprintingState;
        _nextStepTime = Time.time + (sprinting ? _sprintStepInterval : _walkStepInterval);
        float range = sprinting ? _sprintSoundRange : _walkSoundRange;
        Debug.Log($"[Footstep] 원격 방출 id={_worldObject.Id} pos={transform.position} range={range} moveSqr={_worldObject.PlanarMoveSqr:F2}");
        SoundEvent.Emit(transform.position, range, gameObject);
    }
}
