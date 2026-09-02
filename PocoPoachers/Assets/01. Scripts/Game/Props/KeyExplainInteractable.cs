using UnityEngine;

// 상호작용(F)하면 조작키 설명 패널(KeyExplainUI)을 띄운다. 한 번 더 누르면 닫힌다.
// 패널 내용은 씬에 배치된 KeyExplainUI가 갖고 있고, 여기서는 열고 닫기만 한다.
public class KeyExplainInteractable : MonoBehaviour, IInteractable
{
    private PlayerController _player;

    public void OnInteract(PlayerController player)
    {
        var ui = UIManager.GetInstance();
        if (ui == null) return;

        _player = player;

        // ESC로 닫히는 경로(UIManager.HideTop)는 OnInteractExit를 거치지 않는다. 그대로 두면
        // PlayerController가 이 오브젝트를 계속 상호작용 대상으로 붙들고 있어 다음 F가 한 번 먹힌다.
        ui.OnPanelClosed += HandlePanelClosed;
        ui.Show(UIType.KeyExplain);

        // 설명을 읽는 동안 걸어나가거나 쏘지 못하게 전투 입력이 없는 맵으로 돌린다.
        // Inventory가 아니라 ItemBox인 이유는 이 맵에만 Interaction(F)이 남아 있어서다 —
        // Inventory 맵으로 바꾸면 F가 죽어 ESC로만 닫을 수 있게 된다. (Generator도 같은 이유로 ItemBox)
        player.SwitchInputMap(PlayerInputMapType.ItemBox);
    }

    public void OnInteractExit(PlayerController player)
    {
        // Hide가 OnPanelClosed를 쏘므로 반드시 먼저 끊는다 — 안 그러면 아래 정리가 두 번 돈다
        Unsubscribe();
        UIManager.GetInstance()?.Hide(UIType.KeyExplain);
        Restore();
    }

    private void HandlePanelClosed(UIType type)
    {
        if (type != UIType.KeyExplain) return;

        Unsubscribe();
        if (_player != null) _player.EndInteraction(this);
        Restore();
    }

    // Unity의 ==는 파괴된 오브젝트도 null로 보지만 ?.는 걸러내지 못하므로 != null로 검사한다
    private void Restore()
    {
        if (_player != null) _player.SwitchToGameplayInputMap();
        _player = null;
    }

    private void Unsubscribe()
    {
        var ui = UIManager.ExistingInstance;
        if (ui != null) ui.OnPanelClosed -= HandlePanelClosed;
    }

    private void OnDisable() => Unsubscribe();
}
