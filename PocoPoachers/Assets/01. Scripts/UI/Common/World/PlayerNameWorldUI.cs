using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 원격 플레이어 머리 위 이름표. 로컬 플레이어가 반경에 들어왔을 때만 뜬다
// (LocalPlayerProximityTrigger가 Show/Hide를 호출한다 — StatBase가 HpWorldUI를 부르는 것과 같은 방식).
//
// 표시할 이름은 PlayerNameRegistry(호스트가 뿌린 명부)에서 가져온다.
// 위치·오프셋·풀링은 WorldUIManager에 등록된 PlayerName 항목을 따른다.
public class PlayerNameWorldUI : WorldUIBase
{
    [SerializeField] private TextMeshProUGUI _nameText;

    private static readonly Dictionary<int, PlayerNameWorldUI> _active = new Dictionary<int, PlayerNameWorldUI>();

    private int _playerId;
    private Transform _followTarget;

    public static void Show(int playerId, Transform target)
    {
        if (playerId == 0 || target == null) return;
        if (_active.ContainsKey(playerId)) return;

        var ui = WorldUIManager.Instance.Create<PlayerNameWorldUI>(WorldUIType.PlayerName, target);
        if (ui == null) return;

        ui.SetPlayer(playerId, target);
        _active[playerId] = ui;
    }

    public static void Hide(int playerId)
    {
        if (_active.TryGetValue(playerId, out var ui))
            ui.Release();
    }

    private void SetPlayer(int playerId, Transform target)
    {
        _playerId = playerId;
        _followTarget = target;
        Refresh();
    }

    // 원격 플레이어가 디스폰되면 Hide가 오지 않을 수 있다 — 대상이 사라졌으면 스스로 풀에 반납한다
    protected override void LateUpdate()
    {
        base.LateUpdate();

        if (_followTarget == null)
            Release();
    }

    // 이름표가 먼저 뜨고 명부가 나중에 도착할 수 있다 (팀원 캐릭터는 이동 패킷으로 먼저 스폰된다)
    private void OnEnable() => PlayerNameRegistry.OnChanged += Refresh;

    private void OnDisable()
    {
        PlayerNameRegistry.OnChanged -= Refresh;
        _followTarget = null;

        if (_playerId == 0) return;
        _active.Remove(_playerId);
        _playerId = 0;
    }

    private void Refresh()
    {
        if (_nameText == null) return;
        _nameText.text = PlayerNameRegistry.Get(_playerId);
    }
}
