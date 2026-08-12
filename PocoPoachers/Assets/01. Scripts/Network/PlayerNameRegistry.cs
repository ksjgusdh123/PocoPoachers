using System;
using System.Collections.Generic;
using System.Text;

// 방에 있는 모든 플레이어의 닉네임 명부. playerId로 조회한다.
// 원본은 호스트가 들고 있고(H_Roster로 전체 스냅샷 전파), 각 클라이언트는 그 사본을 여기에 둔다.
// 씬을 넘어도 유지돼야 하므로 static — 세션이 새로 시작될 때 Clear()로 비운다.
//
// 이름표/마커는 원격 플레이어 오브젝트가 명부보다 먼저 생길 수 있으므로(이동 패킷이 먼저 도착),
// 생성 시점에 한 번 조회하는 대신 OnChanged를 구독해 갱신해야 한다.
public static class PlayerNameRegistry
{
    const int MaxLength = 12;

    static readonly Dictionary<int, string> _names = new();

    // 호스트가 보낸 명부에 실제로 들어 있던 id. 내 세이브에서 채워 넣은 이름(EnsureLocal)과 구분해야
    // "호스트 방 세계에 내 기록이 있는가"를 판정할 수 있다.
    static readonly HashSet<int> _fromRoster = new();

    public static event Action OnChanged;

    // 호스트 명부에 이 플레이어의 이름이 들어 있었는지 — 게스트의 최초 참여 판정에 쓴다
    public static bool RosterHas(int playerId) => _fromRoster.Contains(playerId);

    // 등록된 이름이 없으면 playerId를 그대로 보여준다 (명부가 아직 안 온 팀원)
    public static string Get(int playerId)
    {
        EnsureLocal();
        return _names.TryGetValue(playerId, out string name) && !string.IsNullOrEmpty(name)
            ? name
            : playerId.ToString();
    }

    public static void Set(int playerId, string nickname)
    {
        string clean = Sanitize(nickname);
        if (string.IsNullOrEmpty(clean)) return;
        if (_names.TryGetValue(playerId, out string old) && old == clean) return;

        _names[playerId] = clean;
        OnChanged?.Invoke();
    }

    // 호스트가 보낸 명부로 통째로 교체한다. 델타가 아니라 전체 스냅샷이라 퇴장한 인원도 함께 정리된다.
    public static void SetAll(IList<int> playerIds, IList<string> nicknames)
    {
        if (playerIds == null || nicknames == null) return;

        _names.Clear();
        _fromRoster.Clear();

        int count = Math.Min(playerIds.Count, nicknames.Count);
        for (int i = 0; i < count; i++)
        {
            string clean = Sanitize(nicknames[i]);
            if (string.IsNullOrEmpty(clean)) continue;

            _names[playerIds[i]] = clean;
            _fromRoster.Add(playerIds[i]);
        }

        OnChanged?.Invoke();
    }

    public static void Remove(int playerId)
    {
        _fromRoster.Remove(playerId);
        if (!_names.Remove(playerId)) return;
        OnChanged?.Invoke();
    }

    // 세션 시작/종료 시 — 이전 방의 명부가 남지 않게 한다
    public static void Clear()
    {
        _fromRoster.Clear();
        if (_names.Count == 0) return;
        _names.Clear();
        OnChanged?.Invoke();
    }

    // 호스트가 뿌릴 명부를 만든다 (자기 자신 + 등록된 게스트 전원)
    public static void BuildRoster(out List<int> playerIds, out List<string> nicknames)
    {
        EnsureLocal();

        playerIds = new List<int>(_names.Count);
        nicknames = new List<string>(_names.Count);
        foreach (var pair in _names)
        {
            playerIds.Add(pair.Key);
            nicknames.Add(pair.Value);
        }
    }

    // 내 닉네임은 패킷이 아니라 내 세이브에서 온다. 아직 명부에 없으면 채워 넣는다.
    // 마스터 서버 없이 시작한 솔로 플레이는 MyPlayerId가 0인데, 마커/이름표도 같은 0을 쓰므로 그대로 키가 된다.
    static void EnsureLocal()
    {
        var nm = NetworkManager.Instance;
        if (nm == null) return;

        int myId = nm.MyPlayerId;
        if (_names.ContainsKey(myId)) return;

        string clean = Sanitize(SaveManager.GetInstance().LoadNickname());
        if (!string.IsNullOrEmpty(clean))
            _names[myId] = clean;
    }

    // 표시용 이름이므로 길이를 자르고 TMP 리치텍스트 태그를 막는다 — '<'를 그대로 두면
    // 닉네임에 넣은 <color=...> 같은 태그가 이름표에서 그대로 해석된다.
    public static string Sanitize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var sb = new StringBuilder(MaxLength);
        foreach (char c in raw.Trim())
        {
            if (c == '<' || c == '>' || char.IsControl(c)) continue;
            sb.Append(c);
            if (sb.Length >= MaxLength) break;
        }
        return sb.Length > 0 ? sb.ToString() : null;
    }
}
