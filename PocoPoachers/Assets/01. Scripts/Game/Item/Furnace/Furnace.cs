using System;
using UnityEngine;

// 광석을 넣어두면 furnace_recipe 테이블의 시간만큼 지난 뒤 주괴로 바꿔주는 시설.
// 투입/결과 칸을 하나씩만 두고, UI를 닫아도 Update가 계속 돌아 제련이 진행된다.
public class Furnace : MonoBehaviour, IInteractable
{
    // 테이블에 0이 들어와도 한 프레임에 무한정 녹지 않도록 하는 하한선
    private const float MinSmeltSeconds = 0.01f;

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

        // 결과 칸이 가득 찼으면 광석을 태우지 않고 그대로 멈춰 기다린다
        if (!CanStoreOutput(recipe)) return;

        _elapsed += Time.deltaTime;
        if (_elapsed >= CurrentDuration)
        {
            _elapsed = 0f;
            Smelt(recipe);
        }

        OnStateChanged?.Invoke();
    }

    // 투입 칸에 광석을 넣는다. 서로 다른 광석은 섞이지 않으므로 먼저 회수해야 한다.
    public bool TryInsertOre(ItemData ore, int amount)
    {
        if (ore == null || amount <= 0) return false;
        if (FurnaceRecipeTable.Instance.Get(ore.id) == null) return false;
        if (InputItem != null && InputItem.id != ore.id) return false;
        if (InputCount + amount > ore.MaxStack) return false;

        if (InputItem == null)
        {
            InputItem = ore;
            _elapsed = 0f;
        }
        InputCount += amount;

        OnStateChanged?.Invoke();
        return true;
    }

    // 아직 안 녹은 광석을 인벤토리로 되돌린다
    public bool TryTakeInput(Inventory inventory)
    {
        if (inventory == null || InputItem == null || InputCount <= 0) return false;
        if (inventory.CanAddItem(InputItem, InputCount) < 0) return false;

        inventory.AddItem(InputItem, InputCount);
        InputItem = null;
        InputCount = 0;
        _elapsed = 0f;

        OnStateChanged?.Invoke();
        return true;
    }

    public bool TryTakeOutput(Inventory inventory)
    {
        if (inventory == null || OutputItem == null || OutputCount <= 0) return false;
        if (inventory.CanAddItem(OutputItem, OutputCount) < 0) return false;

        inventory.AddItem(OutputItem, OutputCount);
        OutputItem = null;
        OutputCount = 0;

        OnStateChanged?.Invoke();
        return true;
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
