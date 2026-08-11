// 레이드 결과 씬(SC_Result)에 넘기는 정보. 결과 씬에는 플레이어가 없어 씬 오브젝트로는 주고받을 수 없다.
// 씬 이동은 호스트가 트리거하고 게스트는 H_LoadScene으로 따라오지만, 결과 내용은 각자 자기 것을 채운다.
// 탈출은 게스트가 기본값(성공 · 쉘터 복귀)을 그대로 쓰고, 전멸은 게스트도 로컬 판정 시점에 실패로 세팅한다.
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
