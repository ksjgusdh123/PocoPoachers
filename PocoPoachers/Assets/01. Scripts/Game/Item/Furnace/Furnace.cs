using System;
using UnityEngine;

// 광석을 넣어두면 furnace_recipe 테이블의 시간만큼 지난 뒤 주괴로 바꿔주는 시설.
// 투입/결과 칸을 하나씩만 두고, UI를 닫아도 제련은 계속 진행된다.
//
// 멀티: 내용물과 제련 진행의 권위는 호스트에게 있다. 게스트는 요청만 보내고 결과를
// H_FurnaceState(내용물)와 H_FurnaceGive(내 인벤으로 들어올 아이템)로 되돌려받는다.
// 솔로는 RoomManager.IsHost가 true라 그대로 호스트 경로를 탄다.
public class Furnace : MonoBehaviour, IInteractable
{
    // 테이블에 0이 들어와도 한 프레임에 무한정 녹지 않도록 하는 하한선
    private const float MinSmeltSeconds = 0.01f;

    // 게스트 요청의 수량 상한 — 다른 G_ 핸들러와 같은 기준의 방어선
    public const int MaxInsertAmount = 99;

    public static Furnace Instance { get; private set; }

    public event Action OnStateChanged;

    public ItemData InputItem { get; private set; }
    public int InputCount { get; private set; }
    public ItemData OutputItem { get; private set; }
    public int OutputCount { get; private set; }

    public bool IsSmelting => InputItem != null && InputCount > 0 && CurrentRecipe != null;
    public float Progress => IsSmelting ? Mathf.Clamp01(_elapsed / CurrentDuration) : 0f;

    private FurnaceRecipeData CurrentRecipe => InputItem != null ? FurnaceRecipeTable.Instance.Get(InputItem.id) : null;

    private float CurrentDuration
    {
        get
        {
            var recipe = CurrentRecipe;
            return recipe != null ? Mathf.Max(MinSmeltSeconds, recipe.SmeltSeconds) : MinSmeltSeconds;
        }
    }

    private float _elapsed;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (InputItem == null || InputCount <= 0) return;

        var recipe = CurrentRecipe;
        if (recipe == null) return;

        // 게스트는 제련하지 않는다 — 게이지가 끊겨 보이지 않게 표시용으로만 시간을 이어 센다.
        // 실제 완성은 호스트가 알려주는 H_FurnaceState로 반영된다.
        if (!RoomManager.IsHost)
        {
            _elapsed = Mathf.Min(_elapsed + Time.deltaTime, CurrentDuration);
            OnStateChanged?.Invoke();
            return;
        }

        // 결과 칸이 가득 찼으면 광석을 태우지 않고 그대로 멈춰 기다린다
        if (!CanStoreOutput(recipe)) return;

        _elapsed += Time.deltaTime;
        if (_elapsed >= CurrentDuration)
        {
            _elapsed = 0f;
            Smelt(recipe);
            BroadcastState();
        }

        OnStateChanged?.Invoke();
    }

    // ── 로컬 플레이어 조작 ────────────────────────────────────

    // 투입 칸에 광석을 넣는다. 서로 다른 광석은 섞이지 않으므로 먼저 회수해야 한다.
    // 게스트는 낙관적으로 먼저 반영하고 호스트에 요청한다 — 거절당하면 H_FurnaceGive로 환불된다.
    public bool TryInsertOre(ItemData ore, int amount)
    {
        if (!CanInsert(ore, amount)) return false;

        ApplyInsert(ore, amount);

        if (RoomManager.IsHost)
            BroadcastState();
        else
            RoomSync.FurnaceInsert(ore.id, amount);

        return true;
    }

    // 아직 안 녹은 광석을 인벤토리로 되돌린다
    public bool TryTakeInput(Inventory inventory) => TryTake(inventory, takeOutput: false);

    public bool TryTakeOutput(Inventory inventory) => TryTake(inventory, takeOutput: true);

    // 게스트는 인벤토리를 먼저 건드리지 않는다. 호스트가 실제로 내줄 수 있는지 판단해
    // H_FurnaceGive로 돌려주는 시점에만 인벤에 들어간다 — 복사/유실 경로를 없애기 위해서다.
    private bool TryTake(Inventory inventory, bool takeOutput)
    {
        ItemData item = takeOutput ? OutputItem : InputItem;
        int count = takeOutput ? OutputCount : InputCount;
        if (item == null || count <= 0) return false;

        if (!RoomManager.IsHost)
        {
            RoomSync.FurnaceTake(takeOutput);
            return true;
        }

        if (inventory == null || inventory.CanAddItem(item, count) < 0) return false;

        ApplyTake(takeOutput);
        inventory.AddItem(item, count);
        BroadcastState();
        return true;
    }

    // ── 상태 변경 (권위 판단과 분리) ──────────────────────────

    private bool CanInsert(ItemData ore, int amount)
    {
        if (ore == null || amount <= 0 || amount > MaxInsertAmount) return false;
        if (FurnaceRecipeTable.Instance.Get(ore.id) == null) return false;
        if (InputItem != null && InputItem.id != ore.id) return false;

        return InputCount + amount <= ore.MaxStack;
    }

    private void ApplyInsert(ItemData ore, int amount)
    {
        if (InputItem == null)
        {
            InputItem = ore;
            _elapsed = 0f;
        }
        InputCount += amount;

        OnStateChanged?.Invoke();
    }

    private void ApplyTake(bool takeOutput)
    {
        if (takeOutput)
        {
            OutputItem = null;
            OutputCount = 0;
        }
        else
        {
            InputItem = null;
            InputCount = 0;
            _elapsed = 0f;
        }

        OnStateChanged?.Invoke();
    }

    private bool CanStoreOutput(FurnaceRecipeData recipe)
    {
        var result = ItemTable.Instance.Get(recipe.ResultItemId);
        if (result == null) return false;
        if (OutputItem != null && OutputItem.id != result.id) return false;

        return OutputCount + recipe.ResultCount <= result.MaxStack;
    }

    private void Smelt(FurnaceRecipeData recipe)
    {
        var result = ItemTable.Instance.Get(recipe.ResultItemId);
        if (result == null) return;

        InputCount--;
        if (InputCount <= 0)
        {
            InputItem = null;
            InputCount = 0;
        }

        OutputItem = result;
        OutputCount += recipe.ResultCount;
    }

    // ── 호스트: 게스트 요청 처리 ──────────────────────────────

    // 게스트가 광석을 넣겠다고 요청. 받아줄 수 없으면 그대로 돌려준다 —
    // 게스트는 이미 자기 인벤에서 뺀 상태라 환불하지 않으면 아이템이 사라진다.
    public void HandleGuestInsert(int guestId, int itemId, int amount)
    {
        var ore = ItemTable.Instance.Get(itemId);

        if (!CanInsert(ore, amount))
        {
            if (ore != null && amount > 0 && amount <= MaxInsertAmount)
                RoomSync.FurnaceGive(guestId, itemId, amount);

            // 게스트는 이미 자기 화면에 넣은 걸로 반영해뒀다 — 되돌리도록 실제 상태를 알려준다
            SendStateToGuest(guestId);
            return;
        }

        ApplyInsert(ore, amount);
        BroadcastState();
    }

    public void HandleGuestTake(int guestId, bool takeOutput)
    {
        ItemData item = takeOutput ? OutputItem : InputItem;
        int count = takeOutput ? OutputCount : InputCount;
        if (item == null || count <= 0) return;

        ApplyTake(takeOutput);
        BroadcastState();
        RoomSync.FurnaceGive(guestId, item.id, count);
    }

    // ── 게스트: 호스트 상태 반영 ──────────────────────────────

    public void ApplyState(int inputItemId, int inputCount, int outputItemId, int outputCount, float elapsed)
    {
        InputItem = inputCount > 0 ? ItemTable.Instance.Get(inputItemId) : null;
        InputCount = InputItem != null ? inputCount : 0;
        OutputItem = outputCount > 0 ? ItemTable.Instance.Get(outputItemId) : null;
        OutputCount = OutputItem != null ? outputCount : 0;
        _elapsed = Mathf.Max(0f, elapsed);

        OnStateChanged?.Invoke();
    }

    // ── 전파 ──────────────────────────────────────────────────

    private void BroadcastState()
    {
        RoomSync.FurnaceState(InputItem?.id ?? 0, InputCount, OutputItem?.id ?? 0, OutputCount, _elapsed);
    }

    // 게스트 입장/씬 진입 스냅샷 (RoomManager.SendWorldObjectsToGuest에서 호출)
    public void SendStateToGuest(int guestId)
    {
        RoomSync.FurnaceStateTo(guestId, InputItem?.id ?? 0, InputCount, OutputItem?.id ?? 0, OutputCount, _elapsed);
    }

    // ── 상호작용 ──────────────────────────────────────────────

    public void OnInteract(PlayerController player)
    {
        player.SetInventoryOpen(true);
        UIManager.GetInstance().Show(UIType.Furnace);

        var ui = player.GetFurnaceUI;
        ui?.SendMessage("Open", player, SendMessageOptions.DontRequireReceiver);

        player.SwitchInputMap(PlayerInputMapType.ItemBox);
    }

    public void OnInteractExit(PlayerController player)
    {
        UIManager.GetInstance().Hide(UIType.Furnace);
        player.SetInventoryOpen(false);

        player.SwitchToGameplayInputMap();
        UIManager.GetInstance().ChangeMouseCursor(true);
    }
}
