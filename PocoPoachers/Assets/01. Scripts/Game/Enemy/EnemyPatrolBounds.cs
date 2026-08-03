using UnityEngine;

// EnemySpawner가 스폰 직후 설정하는 정찰 기준점/반경.
// 타깃이 없을 때(Patrol) 이 위치를 중심으로 Radius 밖을 벗어나지 않도록 제한한다.
public class EnemyPatrolBounds : MonoBehaviour
{
    public Vector3 Origin { get; private set; }
    public float Radius { get; private set; } = -1f; // -1이면 미설정

    public bool IsSet => Radius >= 0f;

    public void SetBounds(Vector3 origin, float radius)
    {
        Origin = origin;
        Radius = radius;
    }
}
