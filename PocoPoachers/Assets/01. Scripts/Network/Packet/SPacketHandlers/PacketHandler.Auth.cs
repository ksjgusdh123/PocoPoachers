
public static partial class PacketHandlers
{
    public static void OnS_LoginResult(FlatPacket root)
    {
        var packet = root.TypeAsS_LoginResult();
        bool success = packet.Success;
        int playerId = packet.PlayerId;

        MainThreadDispatcher.Enqueue(() =>
        {
            NetworkManager.Instance?.OnLoginResult(success, playerId);
        });
    }
}
