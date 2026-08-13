using UnityEngine;

// 씬 밖으로 내보내는 지점의 공통부 — 목적지 결정과 전환 실행만 담당한다.
// 언제 발동하는지(상호작용 / 조건 충족)는 파생 클래스가 정한다.
public abstract class SceneExitBase : MonoBehaviour
{
    public enum TargetScene { Shelter, RaidTest, Title }

    [SerializeField] protected TargetScene _targetScene = TargetScene.Shelter;
    [SerializeField] protected SpawnId _spawnId = SpawnId.None;
    [SerializeField] protected bool _showResultUI = false; // 탈출 성공 연출을 띄운 뒤 이동할지 (레이드 탈출 지점)

    [SerializeField, Tooltip("끄면 로딩 화면을 거치지 않고 곧바로 이동한다. 대신 게스트에게 전파되지 않는 로컬 이동이 되므로 혼자 하는 구간(튜토리얼 등)에만 끌 것.")]
    protected bool _useLoadingScreen = true;

    [SerializeField, Tooltip("도착한 씬에서 눈을 뜨는 연출을 재생한다 — 어둡고 흐릿하게 시작해 서서히 또렷해진다.")]
    protected bool _wakeUpOnArrive = false;

    [SerializeField, Tooltip("눈을 뜨는 연출 길이(초)")]
    protected float _wakeUpDuration = 2f;

    [SerializeField, Tooltip("나가기 전에 플레이어의 인벤토리와 장착 장비를 전부 비운다. 튜토리얼에서 주운 것을 본편으로 가져가지 않게 하는 용도.")]
    protected bool _clearInventoryOnExit = false;

    protected string TargetSceneName => ToSceneName(_targetScene);

    // 실제 이탈. 팀 공용 전환이라 호스트면 게스트도 함께 이동한다.
    protected void Exit()
    {
        SoundManager.GetInstance().PlaySfx("sfx_portal");

        if (_clearInventoryOnExit) ClearPlayerItems();

        // 타이틀은 방을 떠나는 로컬 동작이라 게스트에 전파하지 않는다
        if (_targetScene == TargetScene.Title)
        {
            GameManager.Instance.SetSpawnId(_spawnId);
            SceneLoader.Instance.LoadTitleScene();
            return;
        }

        if (_showResultUI && TryShowResult()) return;

        // 목적지 씬이 로드되는 시점에 재생되도록 미리 예약해둔다
        if (_wakeUpOnArrive)
            ScreenWakeUp.PlayOnSceneLoaded(TargetSceneName, _wakeUpDuration);

        if (!_useLoadingScreen)
        {
            GameManager.Instance?.SetSpawnId(_spawnId);
            SceneLoader.Instance?.LoadSceneDirect(TargetSceneName);
            return;
        }

        SceneTransition.Go(TargetSceneName, _spawnId);
    }

    // 장착 중인 것을 먼저 벗겨야 인벤토리를 비웠을 때 손에 든 무기가 남지 않는다.
    // 씬을 나가면서 저장되므로, 여기서 비운 상태가 그대로 세이브에 기록된다.
    private void ClearPlayerItems()
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        foreach (var equip in player.GetComponents<EquipableController>())
            equip.UnequipAll();

        foreach (var inventory in player.GetComponents<Inventory>())
            inventory.Clear();
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
