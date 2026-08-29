using UnityEngine;

// 내 플레이어의 사망/부활 효과음. HP가 0이 되는 순간(StatBase.Die)과 되살아나는 순간(StatBase.Revive)에 울린다.
// 2D로 재생해 내 화면에서만 들린다 — 팀원의 사망/부활은 소리를 내지 않는다.
public static class PlayerLifeSfx
{
    private const string DIE_KEY = "sfx_player_die";
    private const string REVIVE_KEY = "sfx_player_revive";

    public static void PlayDie() => Play(DIE_KEY);
    public static void PlayRevive() => Play(REVIVE_KEY);

    private static void Play(string key) => SoundManager.GetInstance()?.PlaySfx(key);
}
