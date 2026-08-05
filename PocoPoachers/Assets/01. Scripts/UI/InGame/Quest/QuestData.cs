using System;
using UnityEngine;

public enum QuestState
{
    Available,
    InProgress,
    Completed,
}

// 퀘스트 한 건의 데이터. 아직 QuestTable/QuestManager가 없어 QuestListUI 인스펙터에 직접 채우는 임시 형태 —
// 나중에 DataTable(CSV) 기반으로 옮길 때 필드 구성은 그대로 참고 가능하다.
[Serializable]
public class QuestData
{
    public int Id;
    public string NpcName;
    public string QuestName;
    [TextArea] public string Description;
    public string Goal;
    public string Reward;
    public QuestState State = QuestState.Available;
}
