public static class SceneName
{
    public const string Title = "SC_Title";
    public const string CharacterCreate = "SC_CharacterCreate";
    public const string Loading = "SC_Loading";
    public const string Shelter = "SC_RocketShelter";
    public const string Tutorial = "SC_Tutorial";
    public const string RaidTest = "SC_Raid_1001";
    public const string Result = "SC_Result";

    // 전투가 없는 안전지대(쉘터) 씬인지 — 크로스헤어/전투 등 전투 전용 요소 판별에 사용
    public static bool IsShelter(string sceneName) => sceneName == Shelter;

    // 배터리가 닳지 않는 씬인지 — 쉘터는 전투가 없어서, 튜토리얼은 조작을 익히는 동안
    // 방전으로 죽지 않게 하려고 제외한다. IsShelter와는 별개다(튜토리얼은 전투 씬이라
    // 크로스헤어·전투 입력맵은 그대로 있어야 한다).
    public static bool IsBatterySafe(string sceneName) => sceneName == Shelter || sceneName == Tutorial;
}