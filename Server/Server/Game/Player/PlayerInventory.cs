namespace Server;

public class PlayerInventory
{
    private readonly object _lock = new();
    private readonly Dictionary<int, int> _items = new(); // itemId → amount

    public bool AddItem(int itemId, int amount = 1)
    {
        if (itemId <= 0 || amount <= 0) return false;
        lock (_lock)
        {
            _items.TryGetValue(itemId, out int cur);
            _items[itemId] = cur + amount;
        }
        return true;
    }

    public bool RemoveItem(int itemId, int amount = 1)
    {
        if (itemId <= 0 || amount <= 0) return false;
        lock (_lock)
        {
            if (!_items.TryGetValue(itemId, out int cur) || cur < amount) return false;
            int after = cur - amount;
            if (after == 0) _items.Remove(itemId);
            else _items[itemId] = after;
        }
        return true;
    }

    public bool HasItem(int itemId, int amount = 1)
    {
        lock (_lock)
        {
            return _items.TryGetValue(itemId, out int cur) && cur >= amount;
        }
    }

    public Dictionary<int, int> GetSnapshot()
    {
        lock (_lock) { return new Dictionary<int, int>(_items); }
    }
}
