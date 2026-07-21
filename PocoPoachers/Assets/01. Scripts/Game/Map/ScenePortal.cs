using UnityEngine;

public class ScenePortal : MonoBehaviour, IInteractable
{
    public enum TargetScene { Shelter, RaidTest, Title }

    [SerializeField] private TargetScene _targetScene = TargetScene.Shelter;
    [SerializeField] private SpawnId _spawnId = SpawnId.None;

    public void OnInteract(PlayerController player)
    {
        SoundManager.GetInstance().PlaySfx("sfx_portal");

        // 타이틀은 방을 떠나는 로컬 동작이라 게스트에 전파하지 않는다
        if (_targetScene == TargetScene.Title)
        {
            GameManager.Instance.SetSpawnId(_spawnId);
            SceneLoader.Instance.LoadTitleScene();
            return;
        }

        // 셸터/레이드는 팀 공용 전환 — 호스트면 게스트도 함께 이동한다
        SceneTransition.Go(ToSceneName(_targetScene), _spawnId);
    }

    public void OnInteractExit(PlayerController player) { }

    private static string ToSceneName(TargetScene target) => target switch
    {
        TargetScene.Shelter  => SceneName.Shelter,
        TargetScene.RaidTest => SceneName.RaidTest,
        _                    => null,
    };
}
