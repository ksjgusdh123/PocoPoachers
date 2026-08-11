using System;
using System.Collections.Generic;
using UnityEngine;

// 탈출 구역의 현재 상태. 아이콘 순서는 0번이 호스트, 그 뒤가 게스트(플레이어 id 오름차순)다.
public readonly struct EscapeStatus
{
    public readonly IReadOnlyList<bool> Inside;   // 인원별 구역 안 여부
    public readonly bool  Charging;               // 전원 집결 — 게이지 충전 중
    public readonly float Duration;               // 필요 시간
    public readonly int   ZoneId;                 // 어느 구역의 상태인지

    public EscapeStatus(IReadOnlyList<bool> inside, bool charging, float duration, int zoneId)
    {
        Inside   = inside;
        Charging = charging;
        Duration = duration;
        ZoneId   = zoneId;
    }
}

// 레이드 탈출 지점 — 살아있는 팀원 전원이 구역 안에 모여 일정 시간 버티면 발동한다.
// 판정은 호스트만 한다. 호스트는 G_Move/StatSync로 전원의 위치와 생존 상태를 이미 알고 있어
// 위치 보고용 패킷이 따로 필요 없다. 게스트는 H_EscapeState로 알림 UI만 맞춘다.
[RequireComponent(typeof(Collider))]
public class EscapeZone : SceneExitBase
{
    [SerializeField] private float _requiredSeconds = 5f;

    [SerializeField, Tooltip("전원 위치를 검사하는 간격. 매 프레임 돌 필요가 없다.")]
    private float _pollInterval = 0.15f;

    // EscapeNoticeUI가 구독한다. 호스트는 자기 판정으로, 게스트는 호스트가 보낸 상태로 발행한다.
    // 구역이 여러 개일 수 있어 어느 구역의 신호인지(zoneId) 함께 넘긴다.
    public static event Action<EscapeStatus> OnStatusChanged;
    public static event Action<int>          OnStatusCleared;

    private readonly List<StatBase>    _players  = new();
    private readonly List<int>         _ids      = new();
    private readonly List<WorldObject> _remotes  = new();
    private readonly List<bool>        _inside   = new();
    private readonly List<bool>        _lastSent = new();

    private Collider _zone;
    private int   _zoneId;
    private float _chargeStartTime;
    private float _nextPollTime;
    private bool  _charging;
    private bool  _completed;

    // 마지막으로 알린 상태 — 달라졌을 때만 UI 갱신과 브로드캐스트를 한다.
    private bool _lastActive;
    private bool _lastCharging;

    private void Awake()
    {
        _zone = GetComponent<Collider>();
        AssignZoneIds();
    }

    // 구역 번호는 호스트와 게스트가 같아야 한다. 모두 같은 씬을 로드하므로 이름(같으면 위치) 순으로
    // 정렬해 번호를 매기면 별도 동기화 없이 일치한다.
    private static void AssignZoneIds()
    {
        var zones = FindObjectsByType<EscapeZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Array.Sort(zones, CompareZones);

        for (int i = 0; i < zones.Length; i++)
            zones[i]._zoneId = i;
    }

    private static int CompareZones(EscapeZone a, EscapeZone b)
    {
        int byName = string.CompareOrdinal(a.name, b.name);
        if (byName != 0) return byName;

        Vector3 pa = a.transform.position;
        Vector3 pb = b.transform.position;
        int byX = pa.x.CompareTo(pb.x);
        if (byX != 0) return byX;
        int byY = pa.y.CompareTo(pb.y);
        return byY != 0 ? byY : pa.z.CompareTo(pb.z);
    }

    // 씬을 벗어날 때 알림을 켜둔 채로 남기지 않는다.
    private void OnDisable()
    {
        if (!_lastActive) return;

        _lastActive = false;
        _charging   = false;
        OnStatusCleared?.Invoke(_zoneId);
    }

    // 에디터에서 컴포넌트를 붙일 때의 기본값.
    // 결과창은 결과 씬이 띄우므로 여기서는 켜지 않는다. _targetScene은 "결과창을 닫은 뒤 갈 곳"이다.
    private void Reset()
    {
        _targetScene  = TargetScene.Shelter;
        _showResultUI = false;
    }

    private void Update()
    {
        if (_completed || !RoomManager.IsHost) return;
        if (Time.time < _nextPollTime) return;
        _nextPollTime = Time.time + _pollInterval;

        Evaluate();

        if (_charging && Time.time - _chargeStartTime >= _requiredSeconds) Complete();
    }

    // 인원별 구역 안 여부를 다시 계산하고, 달라졌을 때만 UI 갱신과 브로드캐스트를 한다.
    private void Evaluate()
    {
        CollectPlayers();

        _inside.Clear();
        bool anyInside = false;
        bool allInside = _players.Count > 0;

        for (int i = 0; i < _players.Count; i++)
        {
            // 다운(구조 대기)된 팀원은 살려서 데려와야 한다. 완전 사망자는 CollectPlayers에서 이미 빠진다.
            bool ready = !_players[i].IsDead && IsInside(_players[i].transform.position);
            _inside.Add(ready);

            if (ready) anyInside = true;
            else       allInside = false;
        }

        if (allInside && !_charging)
        {
            _charging = true;
            _chargeStartTime = Time.time;
        }
        else if (!allInside)
        {
            // 한 명이라도 빠지면 처음부터 다시 채운다
            _charging = false;
        }

        if (!HasChanged(anyInside)) return;

        _lastActive   = anyInside;
        _lastCharging = _charging;
        _lastSent.Clear();
        _lastSent.AddRange(_inside);

        Publish(anyInside ? _inside : null, _ids, _charging, _requiredSeconds, _zoneId);
        RoomSync.EscapeState(anyInside, _requiredSeconds, completed: false, _inside, _charging, _ids, _zoneId);
    }

    private bool HasChanged(bool anyInside)
    {
        if (anyInside != _lastActive || _charging != _lastCharging) return true;
        if (_lastSent.Count != _inside.Count) return true;

        for (int i = 0; i < _inside.Count; i++)
            if (_inside[i] != _lastSent[i]) return true;

        return false;
    }

    // 완전 사망(관전)한 인원은 아예 목록에서 뺀다 — 3명 중 1명이 죽었으면 남은 2명만 모이면 된다.
    private void CollectPlayers()
    {
        _players.Clear();
        _ids.Clear();

        // 아이콘 순서를 고정하기 위해 호스트(로컬)를 항상 0번에 둔다
        var local = PlayerMovement.LocalTransform;
        if (local != null)
        {
            int localId = NetworkManager.Instance != null ? NetworkManager.Instance.MyPlayerId : 0;
            var localStat = local.GetComponentInChildren<StatBase>();
            if (localStat != null && !IsGone(localId, localStat))
            {
                _players.Add(localStat);
                _ids.Add(localId);
            }
        }

        var objectManager = ObjectManager.Instance;
        if (objectManager == null) return;

        _remotes.Clear();
        foreach (var worldObject in objectManager.GetAllByKind(ObjectKind.Player))
            if (worldObject != null) _remotes.Add(worldObject);

        // ObjectManager의 열거 순서는 보장되지 않는다. id로 정렬해 아이콘 위치가 튀지 않게 한다.
        _remotes.Sort((a, b) => a.Id.CompareTo(b.Id));

        foreach (var worldObject in _remotes)
        {
            var stat = worldObject.GetComponent<StatBase>();
            if (stat == null || IsGone(worldObject.Id, stat)) continue;

            _players.Add(stat);
            _ids.Add(worldObject.Id);
        }
    }

    // 구조 시간이 끝나 관전으로 넘어간 인원. 되살아났으면(HP > 0) 기록이 남아 있어도 다시 대상이다.
    private static bool IsGone(int playerId, StatBase stat) =>
        stat.CurrentHp <= 0f && PlayerDeathTracker.IsFinalized(playerId);

    // 콜라이더 모양 그대로 판정한다. 안에 있으면 ClosestPoint가 그 점을 그대로 돌려준다.
    private bool IsInside(Vector3 position) =>
        (_zone.ClosestPoint(position) - position).sqrMagnitude < 0.0001f;

    private void Complete()
    {
        _completed  = true;
        _charging   = false;
        _lastActive = false;

        OnStatusCleared?.Invoke(_zoneId);

        // 게스트도 같은 시점에 연출을 시작하도록 먼저 알린다.
        RoomSync.EscapeState(active: false, _requiredSeconds, completed: true, null, charging: false, null, _zoneId);

        // 결과는 레이드 씬 위가 아니라 결과 씬에서 보여준다. 닫기를 누르면 거기서 목적지로 보낸다.
        RaidResultCarry.Set(success: true, TargetSceneName, _spawnId);

        // 포드 호송 연출이 끝난 뒤 팀 전체를 결과 씬으로 옮긴다.
        PlayEscapeSequence(() => SceneTransition.Go(SceneName.Result, SpawnId.None));
    }

    // 게스트 — 호스트가 알려준 상태를 그대로 반영한다.
    public static void ApplyRemoteState(bool active, float duration, bool completed, bool[] inside, bool charging, int[] memberIds, int zoneId)
    {
        if (RoomManager.IsHost) return;

        if (completed)
        {
            // 게스트는 연출만 재생하고, 결과 씬 이동은 호스트의 H_LoadScene을 따른다.
            OnStatusCleared?.Invoke(zoneId);
            PlayEscapeSequence(null);
            return;
        }

        Publish(active ? inside : null, memberIds, charging, duration, zoneId);
    }

    // 구역 밖에 있는 사람에게는 아무것도 띄우지 않는다 — 자기 항목이 안에 있을 때만 알림을 켠다.
    private static void Publish(IReadOnlyList<bool> inside, IReadOnlyList<int> memberIds, bool charging, float duration, int zoneId)
    {
        if (inside == null || inside.Count == 0 || !IsSelfInside(inside, memberIds))
        {
            OnStatusCleared?.Invoke(zoneId);
            return;
        }

        OnStatusChanged?.Invoke(new EscapeStatus(inside, charging, duration, zoneId));
    }

    private static bool IsSelfInside(IReadOnlyList<bool> inside, IReadOnlyList<int> memberIds)
    {
        if (memberIds == null) return false;

        int myId = NetworkManager.Instance != null ? NetworkManager.Instance.MyPlayerId : 0;
        for (int i = 0; i < memberIds.Count && i < inside.Count; i++)
            if (memberIds[i] == myId) return inside[i];

        return false;
    }

    // 탈출이 확정되면 사망 때와 같은 포드 호송 연출을 전원에게 재생한다.
    // 완료 신호는 모두가 받으므로 원격 팀원 몫도 각 클라이언트가 로컬로 재생한다 — 추가 패킷이 필요 없다.
    private static void PlayEscapeSequence(Action onFinished)
    {
        var objectManager = ObjectManager.Instance;
        if (objectManager != null)
        {
            foreach (var worldObject in objectManager.GetAllByKind(ObjectKind.Player))
            {
                if (worldObject == null) continue;

                // 이미 호송된(완전 사망) 팀원에게 다시 포드를 보내지 않는다
                var stat = worldObject.GetComponent<StatBase>();
                if (stat != null && IsGone(worldObject.Id, stat)) continue;

                var remoteEffect = new GameObject("RescueBeamEffect").AddComponent<RescueBeamEffect>();
                remoteEffect.Play(worldObject.transform, null);
            }
        }

        var local = PlayerMovement.LocalTransform;
        var controller = local != null ? local.GetComponent<PlayerController>() : null;
        int myId = NetworkManager.Instance != null ? NetworkManager.Instance.MyPlayerId : 0;

        // 관전 중(이미 호송된) 플레이어는 연출 없이 결과창으로 넘어간다
        if (controller == null || PlayerDeathTracker.IsFinalized(myId))
        {
            onFinished?.Invoke();
            return;
        }

        controller.PlayEscapeBeam(onFinished);
    }
}
