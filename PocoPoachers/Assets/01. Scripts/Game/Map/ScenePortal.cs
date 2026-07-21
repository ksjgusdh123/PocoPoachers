using UnityEngine;

public class ScenePortal : MonoBehaviour, IInteractable
{
    public enum TargetScene { Shelter, RaidTest, Title }

    [SerializeField] private TargetScene _targetScene = TargetScene.Shelter;
    [SerializeField] private SpawnId _spawnId = SpawnId.None;
    [SerializeField] private bool _showResultUI = false; // 탈출 성공 연출을 띄운 뒤 이동할지 (레이드 탈출 포털)

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

        // 탈출 포털: 성공 UI를 띄우고, 확정 시 이동한다
        if (_showResultUI)
        {
            var resultUI = FindAnyObjectByType<RaidResultUI>(FindObjectsInactive.Include);
            if (resultUI != null)
            {
                string target = ToSceneName(_targetScene);
                SpawnId spawn = _spawnId;
                resultUI.ShowSuccess(() => SceneTransition.Go(target, spawn));
                return;
            }
        }

        // 그 외: 셸터/레이드 팀 공용 전환 — 호스트면 게스트도 함께 이동한다
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
