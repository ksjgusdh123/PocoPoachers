public static partial class PacketHandlers
{
    public static void OnH_RescueBeamPlay(FlatPacket root)
    {
        var packet = root.TypeAsH_RescueBeamPlay();
        int playerId = packet.PlayerId;

        // 자기 자신의 사망은 이미 로컬에서 재생 중이므로 중복 재생하지 않는다
        if (playerId == NetworkManager.Instance?.MyPlayerId) return;

        MainThreadDispatcher.Enqueue(() =>
        {
            if (ObjectManager.Instance == null) return;
            if (!ObjectManager.Instance.TryGet(ObjectKind.Player, playerId, out var playerObj)) return;

            var effect = new UnityEngine.GameObject("RescueBeamEffect").AddComponent<RescueBeamEffect>();
            effect.Play(playerObj.transform, null);
        });
    }
}
