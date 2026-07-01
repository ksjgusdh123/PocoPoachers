using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnH_GuestJoined(FlatPacket root)
    {
        var pkt = root.TypeAsH_GuestJoined();

        var rm = RoomManager.Instance;
        if (rm != null)
        {
            if (pkt.InfoLength == 1)
                rm.AddMember();
            else if (RoomManager.MemberCount <= 1)
                rm.SetMemberCount(pkt.InfoLength + 1);
        }

        for (int i = 0; i < pkt.InfoLength; i++)
        {
            if (!pkt.Info(i).HasValue) continue;
            var info = pkt.Info(i).Value;
            var p = info.Pos;

            ObjectManager.Instance?.QueueMove(
                ObjectKind.Player, info.PlayerId,
                new Vector3(p.X, p.Y, p.Z), info.Rotation, 0);
        }
    }

    public static void OnH_Leave(FlatPacket root)
    {
        var pkt = root.TypeAsH_Leave();
        if (pkt.IsHost)
            RoomManager.Instance?.HandleHostLeft();
        else
        {
            ObjectManager.Instance?.Despawn(ObjectKind.Player, pkt.PlayerId);
            RoomManager.Instance?.RemoveMember();
        }
    }

    public static void OnH_ShelterLevel(FlatPacket root)
    {
        var pkt = root.TypeAsH_ShelterLevel();
        ShelterManager.GetInstance()?.SetLevel(pkt.Level);
    }

    public static void OnH_LoadScene(FlatPacket root)
    {
        var pkt = root.TypeAsH_LoadScene();
        string sceneName = pkt.SceneName;
        if (string.IsNullOrEmpty(sceneName)) return;

        MainThreadDispatcher.Enqueue(() =>
        {
            SceneLoader loader = SceneLoader.Instance;
            if (loader == null) return;

            if (pkt.SpawnId != 0)
                GameManager.Instance?.SetSpawnId((SpawnId)pkt.SpawnId);

            if (sceneName == SceneName.Shelter)
                loader.LoadShelterScene();
            else if (sceneName.StartsWith("SC_Raid_") &&
                     int.TryParse(sceneName.Substring("SC_Raid_".Length), out int planetId))
            {
                GameManager.Instance?.SetSelectedPlanet(planetId);
                loader.LoadPlanetScene(planetId);
            }
        });
    }
}
