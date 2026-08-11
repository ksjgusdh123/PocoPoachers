using UnityEngine;

// 씬 밖으로 내보내는 지점의 공통부 — 목적지 결정과 전환 실행만 담당한다.
// 언제 발동하는지(상호작용 / 조건 충족)는 파생 클래스가 정한다.
public abstract class SceneExitBase : MonoBehaviour
{
    public enum TargetScene { Shelter, RaidTest, Title }

    [SerializeField] protected TargetScene _targetScene = TargetScene.Shelter;
    [SerializeField] protected SpawnId _spawnId = SpawnId.None;
    [SerializeField] protected bool _showResultUI = false; // 탈출 성공 연출을 띄운 뒤 이동할지 (레이드 탈출 지점)

    protected string TargetSceneName => ToSceneName(_targetScene);

    // 실제 이탈. 팀 공용 전환이라 호스트면 게스트도 함께 이동한다.
    protected void Exit()
    {
        SoundManager.GetInstance().PlaySfx("sfx_portal");

        // 타이틀은 방을 떠나는 로컬 동작이라 게스트에 전파하지 않는다
        if (_targetScene == TargetScene.Title)
        {
            GameManager.Instance.SetSpawnId(_spawnId);
            SceneLoader.Instance.LoadTitleScene();
            return;
        }

        if (_showResultUI && TryShowResult()) return;

        SceneTransition.Go(TargetSceneName, _spawnId);
    }

    // 결과 연출을 띄웠으면 true. 이동은 확정 시점으로 미룬다.
    private bool TryShowResult()
    {
        var resultUI = FindAnyObjectByType<RaidResultUI>(FindObjectsInactive.Include);
        if (resultUI == null) return false;

        string target = TargetSceneName;
        SpawnId spawn = _spawnId;
        resultUI.ShowSuccess(() => SceneTransition.Go(target, spawn));
        return true;
    }

    private static string ToSceneName(TargetScene target) => target switch
    {
        TargetScene.Shelter  => SceneName.Shelter,
        TargetScene.RaidTest => SceneName.RaidTest,
        _                    => null,
    };
}
