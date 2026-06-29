# 적 AI

## 아키텍처

Unity **Behavior Graph** 기반. 호스트만 AI를 실행하고, 게스트는 `EnemyNetSync`로 위치·HP·상태를 수신한다.

## 핵심 컴포넌트

| 클래스 | 역할 |
|--------|------|
| `EnemyStat` | `EnemyTable` 데이터 → HP, 방어율, NavMeshAgent 속도 |
| `EnemySpawner` | NavMesh 랜덤 스폰, 무기/헬멧 랜덤 장착 (호스트) |
| `EnemyNetSync` | 스폰/이동/피격/사망 네트워크 동기화 |
| `EnemyItemBoxDropper` | 사망 시 필드 아이템 박스 스폰 |
| `AIWeaponController` | AI 무기 발사 |

## AI 공통

| 컴포넌트 | 역할 |
|----------|------|
| `TargetDetector` | FOV + OverlapSphere 탐지, 피격 시 `ForceSetTarget` |
| `AIRotator` | 타겟 방향 회전 |
| `AISpeech` / `SoundDetector` | 대사·소리 감지 |
| `AIState` / `ChangeAiStateAction` | 상태 전환 |

## AI 스킬 (SkillManager)

스킬은 "언제"는 BT, "어떻게"는 코드, "관리"는 `SkillManager`로 분리한다.

| 요소 | 역할 |
|------|------|
| `SkillManager` | AI마다 1개. 보유 스킬 + 쿨다운 관리. `_skillIds`로 `skill.csv` 행 참조 (AI별 차등) |
| `ISkill` / `SkillFactory` | 스킬 인터페이스 + `skill` 문자열 → 스킬 클래스 생성 |
| `DodgeRollSkill` / `RetreatSkill` / `HealSkill` | 구르기 / 후퇴 / 지연 회복 구현 |
| `SkillContext` | 스킬이 쓰는 참조 묶음 (Agent, Stat, Rotator, Animator, Target, Attacker) |
| `UseSkill` / `CanUseSkill` | BT 브리지 노드 (`SkillId` 지정) |
| `AIDodgeState` | 피격 시 구르기 신호(`WantsToDodge`) 발생 — 반응형 트리거 (쿨다운은 SkillManager 위임) |
| `RetreatPointSet` | 씬 배치 후퇴 지점 모음. 없으면 타겟 반대 방향 후퇴 |

수치 데이터는 `skill.csv` (범용 컬럼 speed/distance/duration/clip_name). [data-tables.md](data-tables.md) 참고.

## Behavior Actions (일부)

| Action | 설명 |
|--------|------|
| `TryDetectTargetAction` | 타겟 탐지 시도 |
| `TryForgetTargetAction` | 타겟 망각 |
| `ReachTargetAction` | 타겟 접근 |
| `RotateToTargetAction` | 타겟 조준 |
| `PatrolRandomPositionAction` | 순찰 |
| `ShotBulletAction` | RPM 기반 연사 |
| `SayAction` | 대사 출력 |

## Behavior Conditions (일부)

| Condition | 설명 |
|-----------|------|
| `IsTargetInAttackRangeCondition` | 공격 사거리 내 |
| `HasBulletCondition` | 탄약 보유 |
| `LineTraceToWallCondition` | 시야 차단 벽 |

## 적 데이터 (`enemy.csv`)

| ID | 유형 | 비고 |
|----|------|------|
| 1 | 일반 | |
| 2 | 정예 | |
| 3 | 보스 | |

주요 필드: `max_hp`, `defense_rate`, `move_speed`, `detect_range`, `attack_range`

## 전투 규칙

- 데미지·사망 처리: **호스트만**
- 게스트 클라이언트: AI 비활성, 동기화된 위치·HP만 표시
- 사망 시 `EnemyItemBoxDropper`가 루트 아이템 박스 생성

## 관련 네트워크 패킷

`H_EnemySpawn`, `H_EnemyMove`, `H_EnemyHit`, `H_EnemyDie` — [multiplayer.md](multiplayer.md) 참고.
