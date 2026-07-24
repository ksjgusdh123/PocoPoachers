using UnityEngine;

// 레이드 진행 통계 — 경과 시간과 로컬 플레이어 킬 수를 집계한다.
// 레이드 씬 진입 시 StartRaid로 초기화·시작, 결과 표시(RaidResultUI) 시 StopRaid로 멈춘다.
// 각 클라이언트가 자기 값을 로컬로 집계한다(시간은 모두, 킬은 호스트 권한 판정 기준).
public class RaidStats : Singleton<RaidStats>
{
    public float ElapsedTime { get; private set; } // 이번 레이드 경과 시간(초)
    public int Kills { get; private set; }          // 로컬 플레이어가 처치한 적 수

    private bool _running;

    // 레이드 시작 — 통계 초기화 후 타이머 시작
    public void StartRaid()
    {
        ElapsedTime = 0f;
        Kills = 0;
        _running = true;
    }

    // 결과 표시 등으로 집계 종료
    public void StopRaid() => _running = false;

    // 로컬 플레이어가 적을 처치했을 때 호출
    public void AddKill()
    {
        if (_running) Kills++;
    }

    private void Update()
    {
        if (_running) ElapsedTime += Time.deltaTime;
    }
}
