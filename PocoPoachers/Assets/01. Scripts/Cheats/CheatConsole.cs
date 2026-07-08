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
    bool _scrollToBottom;
    GUIStyle _logStyle;

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
        _commands["shelter"] = new CheatEntry { Usage = "shelter [level <n>|upgrade|need]", Handler = CmdShelter };
        _commands["god"] = new CheatEntry { Usage = "god [on|off]", Handler = CmdGod };
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

        EnsureLogStyle();

        const float width = 720f;
        const float height = 420f;
        var rect = new Rect(16f, 16f, width, height);

        GUI.Box(rect, "Cheat Console");

        var logRect = new Rect(rect.x + 8f, rect.y + 28f, rect.width - 16f, rect.height - 72f);
        string content = string.Join("\n", _log);
        float contentWidth = logRect.width - 20f;
        float contentHeight = _logStyle.CalcHeight(new GUIContent(content), contentWidth);
        contentHeight = Mathf.Max(contentHeight, logRect.height);

        var viewRect = new Rect(0f, 0f, contentWidth, contentHeight);

        if (_scrollToBottom)
        {
            _scroll.y = Mathf.Max(0f, contentHeight - logRect.height);
            _scrollToBottom = false;
        }

        _scroll = GUI.BeginScrollView(logRect, _scroll, viewRect, false, true);
        GUI.Label(new Rect(0f, 0f, contentWidth, contentHeight), content, _logStyle);
        GUI.EndScrollView();

        GUI.SetNextControlName(InputControlName);
        var inputRect = new Rect(rect.x + 8f, rect.y + rect.height - 36f, rect.width - 16f, 28f);
        _input = GUI.TextField(inputRect, _input);

        if (_focusInput)
        {
            GUI.FocusControl(InputControlName);
            _focusInput = false;
        }
    }

    void EnsureLogStyle()
    {
        if (_logStyle != null)
            return;

        _logStyle = new GUIStyle(GUI.skin.label)
        {
            wordWrap = true,
            richText = false,
            fontSize = 14,
            clipping = TextClipping.Overflow,
        };
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

        int added = GiveItem(inventory, itemData, amount);
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

    // 무적 토글 — 인자 없으면 현재 상태 반전, on/off로 명시 지정
    // 데미지는 호스트에서만 적용되므로(Bullet._applyDamage) 호스트 자기 플레이어에만 유효
    void CmdGod(string[] args)
    {
        if (!RequireHost())
            return;

        var player = FindLocalPlayer();
        var stat = player != null ? player.GetComponent<PlayerStat>() : null;
        if (stat == null)
        {
            Log("플레이어를 찾을 수 없습니다.");
            Log("게임 씬에서 플레이어가 스폰된 뒤 다시 시도하세요.");
            return;
        }

        bool enable = !stat.IsGodMode;
        if (args.Length >= 1)
        {
            if (string.Equals(args[0], "on", StringComparison.OrdinalIgnoreCase))
                enable = true;
            else if (string.Equals(args[0], "off", StringComparison.OrdinalIgnoreCase))
                enable = false;
            else
            {
                Log("사용법: god [on|off]");
                return;
            }
        }

        stat.SetGodMode(enable);
        Log($"[CHEAT] 무적 {(enable ? "ON" : "OFF")}");
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

    void CmdShelter(string[] args)
    {
        if (!RequireHost())
            return;

        var shelter = ShelterManager.GetInstance();

        if (args.Length == 0)
        {
            LogShelterStatus(shelter);
            return;
        }

        if (string.Equals(args[0], "level", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length < 2 || !int.TryParse(args[1], out int level))
            {
                Log("사용법: shelter level <n>");
                return;
            }

            shelter.SetLevel(level);
            Log($"[CHEAT] 쉘터 Lv.{shelter.CurrentLevel} 설정");
            return;
        }

        if (string.Equals(args[0], "upgrade", StringComparison.OrdinalIgnoreCase))
        {
            if (!shelter.ForceUpgradeLevel())
            {
                Log("이미 최대 레벨입니다.");
                return;
            }

            Log($"[CHEAT] 쉘터 Lv.{shelter.CurrentLevel} 업그레이드");
            return;
        }

        if (string.Equals(args[0], "need", StringComparison.OrdinalIgnoreCase))
        {
            GiveNextUpgradeMaterials(shelter);
            return;
        }

        Log("사용법: shelter | shelter level <n> | shelter upgrade | shelter need");
    }

    void LogShelterStatus(ShelterManager shelter)
    {
        Log($"쉘터 Lv.{shelter.CurrentLevel}");

        var next = shelter.GetNextLevelData();
        if (next == null)
        {
            Log("다음 레벨 없음 (최대)");
            return;
        }

        Log($"다음 Lv.{next.ShelterLevel}");
        LogUpgradeRequirement(next, shelter);
    }

    void LogUpgradeRequirement(ShelterData data, ShelterManager shelter)
    {
        TryGetPlayerInventory(out var player);
        var storage = FindStorageInventory();

        if (data.NeedItem1Id != 0)
        {
            int have = shelter.GetCombinedItemCount(player, storage, data.NeedItem1Id);
            Log($"  item {data.NeedItem1Id}: {have} / {data.NeedItem1Count}");
        }

        if (data.NeedItem2Id != 0)
        {
            int have = shelter.GetCombinedItemCount(player, storage, data.NeedItem2Id);
            Log($"  item {data.NeedItem2Id}: {have} / {data.NeedItem2Count}");
        }
    }

    void GiveNextUpgradeMaterials(ShelterManager shelter)
    {
        var next = shelter.GetNextLevelData();
        if (next == null)
        {
            Log("이미 최대 레벨입니다.");
            return;
        }

        if (!TryGetPlayerInventory(out var inventory))
            return;

        GiveUpgradeItem(inventory, next.NeedItem1Id, next.NeedItem1Count);
        GiveUpgradeItem(inventory, next.NeedItem2Id, next.NeedItem2Count);
        Log($"[CHEAT] Lv.{next.ShelterLevel} 업그레이드 재료 지급");
    }

    void GiveUpgradeItem(Inventory inventory, int itemId, int count)
    {
        if (itemId == 0 || count <= 0)
            return;

        var itemData = ItemTable.Instance.Get(itemId);
        if (itemData == null)
        {
            Log($"아이템 ID {itemId}를 찾을 수 없습니다.");
            return;
        }

        int added = GiveItem(inventory, itemData, count);
        if (added < count)
            Log($"item {itemId}: {added}/{count}만 추가됨 (인벤토리 부족)");
    }

    // 스택 불가 아이템(무기/방어구 등)은 정상 스폰과 동일하게 개체별 uid를 발급해 한 칸에 하나씩 넣는다.
    // (uid가 있어야 내구도/장탄수/파츠 상태가 WorldEquipmentManager에 연결됨) — 스택 가능 아이템은 uid 없이 합산.
    static int GiveItem(Inventory inventory, ItemData itemData, int amount)
    {
        if (itemData.MaxStack > 1)
            return inventory.AddItem(itemData, amount);

        int added = 0;
        for (int i = 0; i < amount; i++)
        {
            int slot = FindEmptySlot(inventory);
            if (slot < 0) break;

            int uid = ItemSpawner.AssignItemUid(itemData.id);
            inventory.AddItemAtSlot(slot, itemData, 1, uid);
            added++;
        }
        return added;
    }

    static int FindEmptySlot(Inventory inventory)
    {
        for (int i = 0; i < inventory.CurrentCapacity; i++)
            if (inventory.Slots[i].IsEmpty) return i;
        return -1;
    }

    static Inventory FindStorageInventory()
    {
        var storage = FindAnyObjectByType<Storage>(FindObjectsInactive.Exclude);
        return storage != null ? storage.StorageInventory : null;
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

        _scrollToBottom = true;
    }
}
#endif
