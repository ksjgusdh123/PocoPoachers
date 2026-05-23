// 다른 클라이언트 플레이어의 HP를 추적하는 경량 컴포넌트
public class RemotePlayerStat : StatBase
{
    protected override void Awake()
    {
        base.Awake();
        IsLocalOwner = false;
    }
}
