public static class SceneName
{
    public const string Title = "SC_Title";
    public const string CharacterCreate = "SC_CharacterCreate";
    public const string Loading = "SC_Loading";
    public const string Shelter = "SC_RocketShelter";
    public const string RaidTest = "SC_Raid_1001";
    public const string Result = "SC_Result";

    // 전투가 없는 안전지대(쉘터) 씬인지 — 크로스헤어/전투 등 전투 전용 요소 판별에 사용
    public static bool IsShelter(string sceneName) => sceneName == Shelter;
}