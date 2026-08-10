// 레이드 결과 씬(SC_Result)에 넘기는 정보. 결과 씬에는 플레이어가 없어 씬 오브젝트로는 주고받을 수 없다.
// 게스트는 호스트의 이동 신호(H_LoadScene)만 받고 넘어오므로 기본값(성공 · 쉘터 복귀)을 그대로 쓴다.
public static class RaidResultCarry
{
    public static bool    Success   { get; private set; } = true;
    public static string  NextScene { get; private set; } = SceneName.Shelter;
    public static SpawnId NextSpawn { get; private set; } = SpawnId.FromRaid;

    public static void Set(bool success, string nextScene, SpawnId nextSpawn)
    {
        Success   = success;
        NextScene = string.IsNullOrEmpty(nextScene) ? SceneName.Shelter : nextScene;
        NextSpawn = nextSpawn;
    }
}
