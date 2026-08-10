using UnityEngine;

// 레이드 결과 씬(SC_Result)의 진입점. 씬이 열리면 결과창을 띄우고, 닫기를 누르면 쉘터로 보낸다.
// 팀 이동이므로 닫기 버튼은 호스트에게만 보인다 — 게스트는 호스트가 확정하면 H_LoadScene으로 따라온다.
public class ResultSceneController : MonoBehaviour
{
    [SerializeField] private RaidResultUI _resultUI;

    private void Start()
    {
        // 커서 표시 여부는 전역 상태다. 레이드 씬에서 크로스헤어가 꺼둔 커서를 여기서 되돌리지 않으면
        // 결과 씬엔 크로스헤어가 없어 아무도 켜주지 않는다 (SceneLoader.LoadTitleScene과 같은 처리).
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (_resultUI == null)
            _resultUI = FindAnyObjectByType<RaidResultUI>(FindObjectsInactive.Include);

        if (_resultUI == null)
        {
            Debug.LogWarning("[ResultSceneController] 결과 씬에 RaidResultUI가 없습니다.");
            return;
        }

        bool canConfirm = RoomManager.IsHost;

        if (RaidResultCarry.Success)
            _resultUI.ShowSuccess(GoNext, canConfirm);
        else
            _resultUI.ShowFailure(GoNext);
    }

    private void GoNext() => SceneTransition.Go(RaidResultCarry.NextScene, RaidResultCarry.NextSpawn);
}
