namespace Server;

public class Player
{
    public int PlayerId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public Inventory Inventory { get; } = new();
}
