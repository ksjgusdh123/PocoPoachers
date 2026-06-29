using System.Collections.Generic;
using UnityEngine;

// 씬에 배치하는 후퇴 지점 모음. 런타임 스폰 AI가 RetreatSkill에서 찾아 랜덤 후퇴에 사용한다.
// 씬에 없으면 RetreatSkill은 타겟 반대 방향 후퇴로 폴백한다.
public class RetreatPointSet : MonoBehaviour
{
    [SerializeField] private List<GameObject> _points = new();

    public IReadOnlyList<GameObject> Points => _points;
}
    