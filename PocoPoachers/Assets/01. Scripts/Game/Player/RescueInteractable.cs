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

    private Coroutine _rescueCoroutine;

    // ProgressUI가 구독하는 구출 진행 이벤트 (BaseOre 채광 이벤트와 동일 패턴)
    public static event Action<float> OnRescueStarted;
    public static event Action OnRescueEnded;

    public void OnInteract(PlayerController player)
    {
        if (_rescueCoroutine != null) return;

        OnRescueStarted?.Invoke(_rescueDuration);
        _rescueCoroutine = StartCoroutine(RescueRoutine(player));
    }

    private IEnumerator RescueRoutine(PlayerController player)
    {
        Debug.Log("구출중");

        yield return new WaitForSeconds(_rescueDuration);

        _rescueCoroutine = null;
        Debug.Log("구출 완료");
        OnRescueEnded?.Invoke();
        player.EndInteraction(this);
    }

    public void OnInteractExit(PlayerController player)
    {
        CancelRescue();
    }

    // 구출 도중 대상이 비활성화(구출 성공/완전 사망)되면 코루틴이 멈추므로 게이지도 함께 내린다
    private void OnDisable()
    {
        CancelRescue();
    }

    private void CancelRescue()
    {
        if (_rescueCoroutine == null) return;

        StopCoroutine(_rescueCoroutine);
        _rescueCoroutine = null;
        OnRescueEnded?.Invoke();
    }
}
