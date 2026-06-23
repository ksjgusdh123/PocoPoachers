#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class CheatConsole : Singleton<CheatConsole>
{
    delegate void CheatHandler(string[] args);

    struct CheatEntry
    {
        public string Usage;
        public CheatHandler Handler;
    }

    const int MaxLogLines = 100;
    const string InputControlName = "CheatConsoleInput";

    readonly List<string> _log = new();
    readonly Dictionary<string, CheatEntry> _commands = new(StringComparer.OrdinalIgnoreCase);

    bool _isVisible;
    bool _focusInput;
    string _input = string.Empty;
    Vector2 _scroll;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        RegisterCommands();
        Log("Cheat Console ready. ` 키로 열기/닫기");
    }

    void RegisterCommands()
    {
        _commands["give"] = new CheatEntry { Usage = "give <itemId> [amount]", Handler = CmdGive };
        _commands["clear"] = new CheatEntry { Usage = "clear", Handler = _ => CmdClear() };
        _commands["items"] = new CheatEntry { Usage = "items [limit]", Handler = CmdItems };
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.backquoteKey.wasPressedThisFrame)
        {
            _isVisible = !_isVisible;
            if (_isVisible)
                _focusInput = true;
        }

        if (!_isVisible)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame)
            _isVisible = false;

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            Submit(_input);
            _input = string.Empty;
            _focusInput = true;
        }
    }

    void OnGUI()
    {
        if (!_isVisible)
            return;

        const float width = 640f;
        const float height = 360f;
        var rect = new Rect(16f, 16f, width, height);

        GUI.Box(rect, "Cheat Console");

        var logRect = new Rect(rect.x + 8f, rect.y + 24f, rect.width - 16f, rect.height - 64f);
        var viewRect = new Rect(0f, 0f, logRect.width - 24f, _log.Count * 20f + 8f);

        _scroll = GUI.BeginScrollView(logRect, _scroll, viewRect);
        GUI.Label(new Rect(0f, 0f, viewRect.width, viewRect.height), string.Join("\n", _log));
        GUI.EndScrollView();

        GUI.SetNextControlName(InputControlName);
        var inputRect = new Rect(rect.x + 8f, rect.y + rect.height - 32f, rect.width - 16f, 24f);
        _input = GUI.TextField(inputRect, _input);

        if (_focusInput)
        {
            GUI.FocusControl(InputControlName);
            _focusInput = false;
        }
    }

    void Submit(string line)
    {
        line = line.Trim();
        if (string.IsNullOrEmpty(line))
            return;

        Log("> " + line);

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return;

        if (string.Equals(parts[0], "help", StringComparison.OrdinalIgnoreCase))
        {
            PrintHelp();
            return;
        }

        if (_commands.TryGetValue(parts[0], out var entry))
        {
            var args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();
            entry.Handler(args);
            return;
        }

        Log($"알 수 없는 명령: {parts[0]} (help 입력)");
    }

    void PrintHelp()
    {
        var sb = new StringBuilder("help              명령 목록");
        foreach (var entry in _commands.Values)
            sb.Append('\n').Append(entry.Usage);
        Log(sb.ToString());
    }

    void CmdGive(string[] args)
    {
        if (args.Length < 1)
        {
            Log("사용법: give <itemId> [amount]");
            return;
        }

        if (!int.TryParse(args[0], out int itemId))
        {
            Log("itemId는 숫자여야 합니다.");
            return;
        }

        int amount = 1;
        if (args.Length >= 2 && !int.TryParse(args[1], out amount))
        {
            Log("amount는 숫자여야 합니다.");
            return;
        }

        if (!RequireHost() || !TryGetPlayerInventory(out var inventory))
            return;

        if (amount <= 0)
        {
            Log("수량은 1 이상이어야 합니다.");
            return;
        }

        var itemData = ItemTable.Instance.Get(itemId);
        if (itemData == null)
        {
            Log($"아이템 ID {itemId}를 찾을 수 없습니다.");
            return;
        }

        int added = inventory.AddItem(itemData, amount);
        if (added == 0)
        {
            Log("인벤토리 공간이 부족합니다.");
            return;
        }

        string message = added < amount
            ? $"[CHEAT] item {itemId} x{added} 추가 ({amount - added}개 실패)"
            : $"[CHEAT] item {itemId} x{added} 추가";

        Log(message);
        Debug.Log(message);
    }

    void CmdClear()
    {
        if (!RequireHost() || !TryGetPlayerInventory(out var inventory))
            return;

        inventory.Clear();
        Log("[CHEAT] 인벤토리를 비웠습니다.");
    }

    void CmdItems(string[] args)
    {
        int limit = 20;
        if (args.Length >= 1 && int.TryParse(args[0], out int parsed))
            limit = Mathf.Clamp(parsed, 1, 100);

        var sb = new StringBuilder();
        int count = 0;

        foreach (var item in ItemTable.Instance.All)
        {
            if (count >= limit)
                break;

            sb.AppendLine($"{item.id}\t{item.name}");
            count++;
        }

        Log(sb.ToString().TrimEnd());
        Log($"총 {count}개 표시 (items [limit])");
    }

    bool RequireHost()
    {
        if (RoomManager.IsHost)
            return true;

        Log("호스트만 치트를 사용할 수 있습니다.");
        return false;
    }

    bool TryGetPlayerInventory(out Inventory inventory)
    {
        inventory = null;
        var player = FindLocalPlayer();
        if (player == null)
        {
            Log("플레이어 인벤토리를 찾을 수 없습니다.");
            Log("게임 씬에서 플레이어가 스폰된 뒤 다시 시도하세요.");
            return false;
        }

        inventory = player.GetComponent<Inventory>();
        if (inventory != null)
            return true;

        Log("플레이어 인벤토리를 찾을 수 없습니다.");
        return false;
    }

    static PlayerController FindLocalPlayer()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        PlayerController fallback = null;

        foreach (var player in players)
        {
            fallback ??= player;
            var input = player.GetComponent<PlayerInputHandler>();
            if (input != null && input.isActiveAndEnabled)
                return player;
        }

        return fallback;
    }

    void Log(string message)
    {
        _log.Add(message);
        if (_log.Count > MaxLogLines)
            _log.RemoveAt(0);

        _scroll.y = float.MaxValue;
    }
}
#endif
