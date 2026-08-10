using System;
using System.Collections.Generic;
using UnityEngine;

// 레이드 탈출 지점 — 살아있는 팀원 전원이 구역 안에 모여 일정 시간 버티면 발동한다.
// 판정은 호스트만 한다. 호스트는 G_Move/StatSync로 전원의 위치와 생존 상태를 이미 알고 있어
// 위치 보고용 패킷이 따로 필요 없다. 게스트는 H_EscapeState로 게이지와 결과창만 맞춘다.
[RequireComponent(typeof(Collider))]
public class EscapeZone : SceneExitBase
{
    [SerializeField] private float _requiredSeconds = 5f;

    [SerializeField, Tooltip("전원 위치를 검사하는 간격. 매 프레임 돌 필요가 없다.")]
    private float _pollInterval = 0.15f;

    // 게이지(ProgressUI)가 구독한다. 호스트는 자기 판정으로, 게스트는 호스트가 보낸 상태로 발행한다.
    public static event Action<float> OnChargeStarted;
    public static event Action        OnChargeEnded;

    private readonly List<StatBase> _players = new();

    private Collider _zone;
    private float _chargeStartTime;
    private float _nextPollTime;
    private bool  _charging;
    private bool  _completed;

    private void Awake() => _zone = GetComponent<Collider>();

    // 씬을 벗어날 때 게이지를 켜둔 채로 남기지 않는다.
    private void OnDisable()
    {
        if (!_charging) return;

        _charging = false;
        OnChargeEnded?.Invoke();
    }

    // 에디터에서 컴포넌트를 붙일 때의 기본값 — 탈출 지점은 결과 연출을 띄운다.
    private void Reset()
    {
        _targetScene  = TargetScene.Shelter;
        _showResultUI = true;
    }

    private void Update()
    {
        if (_completed || !RoomManager.IsHost) return;
        if (Time.time < _nextPollTime) return;
        _nextPollTime = Time.time + _pollInterval;

        if (!AllPlayersReady())
        {
            StopCharging();
            return;
        }

        if (!_charging)
        {
            StartCharging();
            return;
        }

        if (Time.time - _chargeStartTime >= _requiredSeconds) Complete();
    }

    // 살아있는 팀원 전원이 구역 안에 있어야 한다. 다운(구조 대기)된 팀원이 있으면 살려서 데려와야 한다.
    private bool AllPlayersReady()
    {
        CollectPlayers();
        if (_players.Count == 0) return false;

        foreach (var stat in _players)
        {
            if (stat.IsDead) return false;
            if (!IsInside(stat.transform.position)) return false;
        }
        return true;
    }

    private void CollectPlayers()
    {
        _players.Clear();

        var local = PlayerMovement.LocalTransform;
        if (local != null)
        {
            var localStat = local.GetComponentInChildren<StatBase>();
            if (localStat != null) _players.Add(localStat);
        }

        var objectManager = ObjectManager.Instance;
        if (objectManager == null) return;

        foreach (var worldObject in objectManager.GetAllByKind(ObjectKind.Player))
        {
            if (worldObject == null) continue;

            var stat = worldObject.GetComponent<StatBase>();
            if (stat != null) _players.Add(stat);
        }
    }

    // 콜라이더 모양 그대로 판정한다. 안에 있으면 ClosestPoint가 그 점을 그대로 돌려준다.
    private bool IsInside(Vector3 position) =>
        (_zone.ClosestPoint(position) - position).sqrMagnitude < 0.0001f;

    private void StartCharging()
    {
        _charging = true;
        _chargeStartTime = Time.time;

        OnChargeStarted?.Invoke(_requiredSeconds);
        RoomSync.EscapeState(active: true, _requiredSeconds, completed: false);
    }

    // 한 명이라도 빠지면 처음부터 다시 채운다.
    private void StopCharging()
    {
        if (!_charging) return;

        _charging = false;
        OnChargeEnded?.Invoke();
        RoomSync.EscapeState(active: false, _requiredSeconds, completed: false);
    }

    private void Complete()
    {
        _completed = true;
        _charging  = false;

        OnChargeEnded?.Invoke();

        // 게스트가 결과창을 먼저 띄우게 알린 뒤 이동 절차(결과 연출 → SceneTransition.Go)를 탄다.
        RoomSync.EscapeState(active: false, _requiredSeconds, completed: true);
        Exit();
    }

    // 게스트 — 호스트가 알려준 상태를 그대로 반영한다.
    public static void ApplyRemoteState(bool active, float duration, bool completed)
    {
        if (RoomManager.IsHost) return;

        if (completed)
        {
            OnChargeEnded?.Invoke();
            ShowGuestResult();
            return;
        }

        if (active) OnChargeStarted?.Invoke(duration);
        else        OnChargeEnded?.Invoke();
    }

    // 이동 시점은 호스트가 정한다(H_LoadScene). 게스트는 결과만 보고 기다리므로 확인 버튼도 띄우지 않는다.
    private static void ShowGuestResult()
    {
        var resultUI = FindAnyObjectByType<RaidResultUI>(FindObjectsInactive.Include);
        if (resultUI == null) return;

        resultUI.ShowSuccess(null, buttonVisible: false);
    }
}
