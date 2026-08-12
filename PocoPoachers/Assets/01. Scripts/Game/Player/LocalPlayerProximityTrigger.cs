using System;
using System.Collections.Generic;
using UnityEngine;

// ── 배치 ────────────────────────────────────────────────────────────────
//  RemotePlayer (Layer: Player)
//  └─ NameRangeBox (Layer: Default, SphereCollider[isTrigger], 이 스크립트)  ← 상시 켜둔다
// ────────────────────────────────────────────────────────────────────────
//
// 원격 플레이어의 자식 트리거에 붙여, "로컬 플레이어가 이 사람 반경에 들어왔는지"를 알린다.
// 이름표처럼 가까운 팀원에게만 뭔가를 보여줄 때 쓴다.
//
// 레이어가 Player면 안 된다 — Layer Collision Matrix에서 Player-Player가 꺼져 있어 트리거가 아예 발생하지 않는다.
// (RescueInteractable의 RescueBox가 Default 레이어인 것과 같은 이유)
//
// 들어온 게 로컬 플레이어인지 반드시 확인한다. 다른 원격 플레이어도 같은 Player 레이어라
// 확인하지 않으면 "내가 아닌 남이 옆에 있을 때" 켜져버린다.
[RequireComponent(typeof(Collider))]
public class LocalPlayerProximityTrigger : MonoBehaviour
{
    public event Action<bool> OnLocalPlayerNearChanged;

    public bool IsLocalPlayerNear { get; private set; }

    // 이 트리거를 달고 있는 원격 플레이어의 id.
    // 스폰 직후엔 아직 배정 전일 수 있어 0이면 계속 다시 읽고, 한 번 받으면 캐시한다 —
    // 디스폰되는 순간에도 id를 알아야 이름표를 정확히 내릴 수 있다.
    public int PlayerId
    {
        get
        {
            if (_playerId == 0)
                _playerId = GetComponentInParent<WorldObject>()?.Id ?? 0;
            return _playerId;
        }
    }

    int _playerId;

    // 로컬 플레이어에 콜라이더가 여러 개 있어도 한 번만 켜지고 꺼지도록 겹친 콜라이더를 센다
    readonly HashSet<Collider> _localColliders = new();

    void OnTriggerEnter(Collider other)
    {
        if (!IsLocalPlayer(other)) return;
        if (!_localColliders.Add(other)) return;
        if (_localColliders.Count != 1) return;

        SetNear(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!_localColliders.Remove(other)) return;
        if (_localColliders.Count != 0) return;

        SetNear(false);
    }

    // 원격 플레이어가 디스폰되거나 씬이 바뀌면 OnTriggerExit이 오지 않으므로 여기서 정리한다
    void OnDisable()
    {
        _localColliders.Clear();
        SetNear(false);
    }

    static bool IsLocalPlayer(Collider other)
    {
        Transform local = PlayerMovement.LocalTransform;
        return local != null && other.transform.root == local.root;
    }

    void SetNear(bool near)
    {
        if (IsLocalPlayerNear == near) return;

        IsLocalPlayerNear = near;

        // 이름표 표시 (StatBase가 HpWorldUI.Show를 직접 부르는 것과 같은 방식)
        if (near) PlayerNameWorldUI.Show(PlayerId, transform.root);
        else      PlayerNameWorldUI.Hide(PlayerId);

        OnLocalPlayerNearChanged?.Invoke(near);
    }
}
