// 씬 전환 공용 로직 — 호스트면 게스트에 H_LoadScene을 전파하고, 로컬에서도 해당 씬을 로드한다.
// ScenePortal(상호작용 이동), 레이드 팀 전멸 복귀 등에서 공통으로 사용한다.
public static class SceneTransition
{
    // 팀 공용 씬 전환: 호스트면 게스트에 전파하고, 자신도 로드한다.
    public static void Go(string sceneName, SpawnId spawnId)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        GameManager.Instance?.SetSpawnId(spawnId);

        if (RoomManager.IsHost && RoomManager.HasGuests)
            PacketBuilder.BroadcastReliableToGuests(
                new H_LoadSceneT { SceneName = sceneName, SpawnId = (int)spawnId },
                H_LoadScene.Pack, PacketType.H_LoadScene);

        LoadLocal(sceneName);
    }

    // 씬 이름 → 로컬 SceneLoader 호출. 호스트 발신부와 게스트 수신부(OnH_LoadScene)가 동일 규칙을 쓰도록 한 곳에 모은다.
    public static void LoadLocal(string sceneName)
    {
        var loader = SceneLoader.Instance;
        if (loader == null) return;

        if (sceneName == SceneName.Shelter)
        {
            loader.LoadShelterScene();
        }
        else if (sceneName == SceneName.Result)
        {
            loader.LoadResultScene();
        }
        else if (sceneName.StartsWith("SC_Raid_") &&
                 int.TryParse(sceneName.Substring("SC_Raid_".Length), out int planetId))
        {
            GameManager.Instance?.SetSelectedPlanet(planetId);
            loader.LoadPlanetScene(planetId);
        }
    }
}
