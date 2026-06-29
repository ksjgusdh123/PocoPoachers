using System.Collections.Generic;
using UnityEngine;

// 씬에 배치하는 후퇴 지점 모음. RetreatSkill이 런타임에 찾아 랜덤 후퇴에 사용 (없으면 반대 방향 폴백).
public class RetreatPointSet : MonoBehaviour
{
    [SerializeField] private List<GameObject> _points = new();

    public IReadOnlyList<GameObject> Points => _points;
}
    