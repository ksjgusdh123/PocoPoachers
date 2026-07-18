using System;
using System.Collections;
using UnityEngine;

// 다운된 플레이어의 자식 트리거 오브젝트에 부착 — 오브젝트가 켜져 있는 동안만 구출 대상이 된다
// Layer Collision Matrix에서 Player-Player 충돌이 꺼져 있어 트리거도 발생하지 않으므로
// 이 오브젝트의 레이어는 Player가 아닌 것(Default 등)이어야 한다
// PlayerController.GetNearestInteractable이 콜라이더가 붙은 오브젝트에서 IInteractable을 찾으므로
// 이 컴포넌트와 트리거 콜라이더는 반드시 같은 오브젝트에 있어야 한다
public class RescueInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private float _rescueDuration = 1f;
    [SerializeField] private ProximityDetector _proximityDetector; // 구출자 근접 감지 (ItemBox/BaseOre와 동일 패턴)

    private Coroutine _rescueCoroutine;
    private UIScalePulse _pulseUI;
    private bool _isPlayerNearby;

    // ProgressUI가 구독하는 구출 진행 이벤트 (BaseOre 채광 이벤트와 동일 패턴)
    // 게이지는 호스트가 되돌려준 H_Rescue로만 뜬다 — 구출자·대상 양쪽에서 같은 경로로 표시된다
    public static event Action<float> OnRescueStarted;
    public static event Action OnRescueEnded;

    public static void RaiseProgress(RescueState state, float duration)
    {
        if (state == RescueState.Started)
            OnRescueStarted?.Invoke(duration);
        else
            OnRescueEnded?.Invoke();
    }

    // 구출 대상의 플레이어 id — 이 컴포넌트는 RemotePlayer의 자식이라 부모에서 찾는다
    private int TargetId => GetComponentInParent<WorldObject>()?.Id ?? 0;

    private void Awake()
    {
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
        _isPlayerNearby = true;
        if (_rescueCoroutine == null) ShowPulse(); // 구출 진행 중이 아닐 때만 표시
    }

    private void OnPlayerExited()
    {
        _isPlayerNearby = false;
        HidePulse();
    }

    private void ShowPulse()
    {
        if (_pulseUI != null) return; // 중복 생성 방지
        _pulseUI = WorldUIManager.Instance.Create<UIScalePulse>(WorldUIType.ScalePulse, transform);
    }

    private void HidePulse()
    {
        _pulseUI?.Release();
        _pulseUI = null;
    }

    public void OnInteract(PlayerController player)
    {
        if (_rescueCoroutine != null) return;

        int targetId = TargetId;
        if (targetId == 0)
        {
            Debug.LogWarning("[RescueInteractable] 대상의 WorldObject를 찾을 수 없어 구출할 수 없습니다", this);
            return;
        }

        RoomSync.Rescue(targetId, RescueState.Started, _rescueDuration);
        HidePulse(); // 구출 진행 중엔 게이지가 뜨므로 펄스는 숨긴다
        _rescueCoroutine = StartCoroutine(RescueRoutine(player));
    }

    // 진행 시간 판정은 구출자 로컬에서만 한다 — 호스트는 이 결과를 받아 양쪽 게이지를 정리한다
    private IEnumerator RescueRoutine(PlayerController player)
    {
        yield return new WaitForSeconds(_rescueDuration);

        _rescueCoroutine = null;
        Debug.Log("구출 완료");
        RoomSync.Rescue(TargetId, RescueState.Completed, _rescueDuration);
        player.EndInteraction(this);
    }

    public void OnInteractExit(PlayerController player)
    {
        CancelRescue();
        if (_isPlayerNearby) ShowPulse(); // 구출을 마치지 못하고 벗어났으면 근처에 있는 한 다시 안내
    }

    // 구출 도중 대상이 비활성화(구출 성공/완전 사망)되면 코루틴이 멈추므로 게이지도 함께 내린다
    private void OnDisable()
    {
        CancelRescue();
        HidePulse();
        _isPlayerNearby = false;
    }

    private void CancelRescue()
    {
        if (_rescueCoroutine == null) return;

        StopCoroutine(_rescueCoroutine);
        _rescueCoroutine = null;
        RoomSync.Rescue(TargetId, RescueState.Cancelled, _rescueDuration);
    }
}
