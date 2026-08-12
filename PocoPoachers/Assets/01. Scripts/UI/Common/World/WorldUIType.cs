public enum WorldUIType
{
    SpeechBubble,
    ScalePulse,
    DamageText,
    HpBar,

    // 기존 값이 밀리지 않도록 새 타입은 반드시 끝에 추가한다 (씬의 WorldUIManager가 int로 저장하고 있다)
    PlayerName
}
