namespace Server;

public class Player
{
    public int PlayerId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public PlayerStat Stat { get; } = new();
    public PlayerInventory Inventory { get; } = new();
}
