// 씬 비동기 로딩 진행률을 보여주는 UI. 값 주입은 LoadingSceneController가 담당.
public class LoadingScreenUI : ProgressUIBase
{
    protected override void Subscribe() { }
    protected override void Unsubscribe() { }

    public void Begin() => Show();
    public void Report(float progress) => SetProgress(progress);
}
