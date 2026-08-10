using System;
using System.Collections.Generic;
using UnityEngine;

public enum MoveVoteCancelReason
{
    Declined,
    TimedOut,
    HostCancelled,
}

// 호스트가 팀 이동을 제안하고 게스트 전원의 수락을 기다리는 절차.
// 전원 수락이 확정되면 기존 SceneTransition.Go 경로(H_LoadScene 브로드캐스트 + 로컬 로드)를 그대로 탄다.
public class SceneMoveVote : Singleton<SceneMoveVote>
{
    const float TIMEOUT_SECONDS = 20f;

    readonly HashSet<int> _pending     = new();
    readonly List<int>    _guestBuffer = new();

    // 아이콘 표시 순서. 응답이 와도 지우지 않아 guestId → 아이콘 위치가 대기 내내 고정된다.
    readonly List<int>    _order       = new();

    string  _sceneName;
    SpawnId _spawnId;
    int     _guestCount;
    float   _deadline;
    int     _shownRemaining = -1;

    public bool IsWaiting { get; private set; }

    // 전용 대기 UI를 따로 붙일 때 쓰는 훅. 구독자가 없어도 절차 자체는 동작한다.
    public static event Action<int>                   OnWaitStarted;    // 대기 중인 게스트 수
    public static event Action<int, int>              OnReplyReceived;  // (수락 수, 전체 수)
    public static event Action<MoveVoteCancelReason>  OnCancelled;

    protected override void Awake()
    {
        base.Awake();
        if (_instance != this) return;

        DontDestroyOnLoad(gameObject);
        RoomManager.OnGuestLeft += HandleGuestLeft;
    }

    protected override void OnDestroy()
    {
        if (_instance == this) RoomManager.OnGuestLeft -= HandleGuestLeft;
        base.OnDestroy();
    }

    // 호스트이고 게스트가 있을 때만 투표를 시작한다. false면 호출부가 기존대로 즉시 이동하면 된다.
    public static bool TryBeginTeamMove(string sceneName, SpawnId spawnId)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        if (!RoomManager.IsHost || !RoomManager.HasGuests) return false;

        return GetInstance().Begin(sceneName, spawnId);
    }

    bool Begin(string sceneName, SpawnId spawnId)
    {
        // 이미 대기 중이면 새 제안으로 덮어쓰지 않는다 (연타/중복 상호작용 방지)
        if (IsWaiting) return true;

        RoomManager.Instance.CollectGuestIds(_guestBuffer);
        if (_guestBuffer.Count == 0) return false;

        _pending.Clear();
        _order.Clear();
        foreach (int guestId in _guestBuffer)
        {
            _pending.Add(guestId);
            _order.Add(guestId);
        }

        _sceneName  = sceneName;
        _spawnId    = spawnId;
        _guestCount = _pending.Count;
        _deadline   = Time.unscaledTime + TIMEOUT_SECONDS;
        IsWaiting   = true;

        RoomSync.MoveRequest(sceneName, spawnId);

        var localization = LocalizationManager.GetInstance();
        var manager = UIManager.GetInstance();
        manager.ShowVoteWaiting(
            localization.GetString("vote.title"),
            string.Format(localization.GetString("vote.waiting_message"), DescribeScene(sceneName)),
            () => Cancel(MoveVoteCancelReason.HostCancelled));
        manager.SetVoteMemberCount(_guestCount);
        RefreshProgress(force: true);

        OnWaitStarted?.Invoke(_guestCount);
        return true;
    }

    // 호스트 — 게스트 응답 수신
    public void HandleGuestReply(int guestId, bool accepted)
    {
        if (!IsWaiting || !_pending.Remove(guestId)) return;

        if (!accepted)
        {
            Cancel(MoveVoteCancelReason.Declined);
            return;
        }

        UIManager.GetInstance().MarkVoteMemberAccepted(_order.IndexOf(guestId));
        RefreshProgress(force: true);
        OnReplyReceived?.Invoke(_guestCount - _pending.Count, _guestCount);

        if (_pending.Count == 0) Commit();
    }

    // 대기 중 나간 게스트는 응답을 기다릴 필요가 없다. 남은 인원이 없으면 그대로 진행한다.
    void HandleGuestLeft(int guestId)
    {
        if (!IsWaiting || !_pending.Remove(guestId)) return;

        UIManager.GetInstance().MarkVoteMemberGone(_order.IndexOf(guestId));
        RefreshProgress(force: true);
        if (_pending.Count == 0) Commit();
    }

    // 게스트 — 호스트의 이동 제안을 띄운다
    public void ShowRequest(string sceneName)
    {
        var localization = LocalizationManager.GetInstance();
        UIManager.GetInstance().ShowVoteRequest(
            localization.GetString("vote.title"),
            string.Format(localization.GetString("vote.request_message"), DescribeScene(sceneName)),
            () => RoomSync.MoveReply(true),
            () => RoomSync.MoveReply(false));
    }

    // 게스트 — 이동이 무산됐다는 통보
    public void HandleCancelled()
    {
        var manager = UIManager.GetInstance();
        manager.HideVote();

        var localization = LocalizationManager.GetInstance();
        manager.ShowNotice(localization.GetString("vote.title"), localization.GetString("vote.cancelled_message"));
    }

    void Update()
    {
        if (!IsWaiting) return;

        // 방을 떠났거나 호스트 권한을 잃으면 대기 상태만 정리한다 (이동은 하지 않는다).
        if (!RoomManager.IsHost)
        {
            ResetState();
            UIManager.GetInstance().HideVote();
            return;
        }

        if (Time.unscaledTime >= _deadline)
        {
            Cancel(MoveVoteCancelReason.TimedOut);
            return;
        }

        RefreshProgress();
    }

    void Commit()
    {
        if (!RoomManager.IsHost)
        {
            ResetState();
            return;
        }

        string sceneName = _sceneName;
        SpawnId spawnId  = _spawnId;
        ResetState();

        UIManager.GetInstance().HideVote();
        SceneTransition.Go(sceneName, spawnId);
    }

    void Cancel(MoveVoteCancelReason reason)
    {
        if (!IsWaiting) return;
        ResetState();

        RoomSync.MoveCancel(reason);

        var manager = UIManager.GetInstance();
        manager.HideVote();

        // 호스트가 스스로 취소한 경우는 결과를 이미 알고 있으므로 알림을 띄우지 않는다.
        if (reason == MoveVoteCancelReason.HostCancelled)
        {
            OnCancelled?.Invoke(reason);
            return;
        }

        var localization = LocalizationManager.GetInstance();
        string messageKey = reason == MoveVoteCancelReason.Declined
            ? "vote.declined_message"
            : "vote.timeout_message";
        manager.ShowNotice(localization.GetString("vote.title"), localization.GetString(messageKey));

        OnCancelled?.Invoke(reason);
    }

    void ResetState()
    {
        IsWaiting = false;
        _pending.Clear();
        _order.Clear();
        _sceneName = null;
        _shownRemaining = -1;
    }

    // 남은 초가 바뀔 때만 갱신한다 — 매 프레임 문자열을 새로 만들면 대기 내내 GC가 쌓인다.
    void RefreshProgress(bool force = false)
    {
        int remaining = Mathf.Max(0, Mathf.CeilToInt(_deadline - Time.unscaledTime));
        if (!force && remaining == _shownRemaining) return;

        _shownRemaining = remaining;
        int accepted = _guestCount - _pending.Count;
        UIManager.GetInstance().SetVoteProgress($"{accepted} / {_guestCount}   {remaining}s");
    }

    // 씬 이름을 플레이어에게 보여줄 이름으로 바꾼다.
    static string DescribeScene(string sceneName)
    {
        const string raidPrefix = "SC_Raid_";
        var localization = LocalizationManager.GetInstance();

        if (sceneName == SceneName.Shelter)
            return localization.GetString("vote.destination_shelter");

        if (sceneName.StartsWith(raidPrefix) &&
            int.TryParse(sceneName.Substring(raidPrefix.Length), out int planetId))
        {
            var data = PlanetTable.Instance?.Get(planetId);
            if (data != null) return localization.GetString(data.PlanetName);
        }

        return sceneName;
    }
}
