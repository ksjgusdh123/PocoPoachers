// 조작키 설명 패널. 내용은 프리팹에 배치돼 있으므로 이 스크립트는 UIManager 등록만 맡는다.
// UIBase를 상속하는 이유는 두 가지다 —
//  1) UIManager가 씬 로드마다 비활성 오브젝트까지 스캔해 등록해준다(SceneUIRegistrar와 동일)
//  2) 에디터에서 패널을 켜둔 채 꾸며도 실행하면 알아서 닫힌 상태로 시작한다
public class KeyExplainUI : UIBase
{
    protected override UIType UiType => UIType.KeyExplain;
}
