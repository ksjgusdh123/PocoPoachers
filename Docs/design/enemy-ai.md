# 적 AI

## 아키텍처

Unity 공식 **Behavior Graph**(`Unity.Behavior`, `BehaviorGraphAgent`) 패키지 기반. 커스텀 FSM/유틸리티 AI가 아니다.
`AIState` enum(Idle/Patrol/Chase/Reload/Attack/Retreat/Rolling)은 BT의 결정 결과를 애니메이터에 반영하기 위한 **보조 상태 변수**일 뿐, 의사결정 주체가 아니다 — `AIStateTransitionRules`가 허용 전이만 검사하고 `ChangeAiStateAction`(BT 액션 노드)이 실제로 값을 바꾼다.

호스트만 AI를 실행한다. `EnemyNetworkSetup.Awake`가 게스트 클라이언트에서 `NavMeshAgent`/`BehaviorGraphAgent`/`TargetDetector`/`AIWeaponController`/`AIRotator`/`SoundDetector`/`EnemyItemBoxDropper`를 전부 비활성화 — 게스트는 AI 로직을 아예 실행하지 않고 표시만 한다.

## 핵심 컴포넌트

| 클래스 | 역할 |
|--------|------|
| `EnemyStat`(`StatBase` 상속) | `EnemyTable`에서 HP·방어율·`NavMeshAgent.speed`만 적용 |
| `EnemySpawner` | 씬 시작 시 1회, NavMesh 랜덤 위치에 배열 스폰. 웨이브·재스폰 없음 (호스트만) |
| `EnemyNetSync` | 스폰/이동(0.1초 간격, 게스트 있을 때만)/피격/사망 동기화, 킬 카운트 전송 |
| `EnemyItemBoxDropper` | 사망 시 필드 아이템 박스 스폰 |
| `AIWeaponController` | AI 무기 발사, 사거리를 `TargetDetector.SetDetectRange`에 역으로 반영 |

### 데이터 배선 갭

`enemy.csv`에는 `detect_range`, `forget_range`, `fov_angle`, `attack_range` 컬럼이 있지만, `EnemyStat.Awake()`는 `max_hp`/`defense_rate`/`move_speed`만 읽는다. 실제 탐지 사거리는 `TargetDetector`의 인스펙터 값이 기본이며, `AIWeaponController.UpdateBlackboardGunStat`이 **장착 무기의 사거리(`BulletRange`)로 덮어쓴다** — CSV의 탐지 관련 컬럼은 현재 아무 코드도 소비하지 않는 죽은 데이터다.

## AI 공통 컴포넌트

| 컴포넌트 | 역할 |
|----------|------|
| `TargetDetector` | `OverlapSphere` + FOV 각도 체크(라인오브사이트 없음) + 생존 필터. 피격 시 `ForceSetTarget`(호스트만) |
| `SoundDetector` | 정적 이벤트 `SoundEvent.OnSoundEmitted` 수신, 더 가까운 소리로만 강제 재타겟 |
| `AIRotator` | 타겟 방향 회전 |
| `AISpeech` | 대사 출력, `H_EnemySpeak`로 원격 재생 |

## Behavior Actions / Conditions (일부)

| Action | 설명 |
|--------|------|
| `TryDetectTargetAction` / `TryForgetTargetAction` | 탐지/망각 시도 |
| `ReachTargetAction` | NavMesh 접근, 벽 레이캐스트 차단 검사(`IsWallBlocking`) |
| `RotateToTargetAction` | 타겟 조준 |
| `PatrolRandomPositionAction` | 순찰, 타겟 발견 시 중단 |
| `ShotBulletAction` | RPM 기반 연사 |
| `HealAfterDelayAction` | 지연 자가 회복 |
| `SayAction` | 대사 출력 |

| Condition | 설명 |
|-----------|------|
| `IsTargetInAttackRangeCondition` | 공격 사거리 내 여부 |
| `IsHpBelowRatioCondition` | HP 비율 미만, `HasRetreated`면 항상 false(후퇴는 생애 1회) |
| `HasBulletCondition` | 탄약 보유 |
| `LineTraceToWallCondition` | 시야 차단 벽 |
| `WantsToDodgeCondition` | `AIDodgeState.WantsToDodge` |

## AI 스킬 (SkillManager)

스킬은 "언제"는 BT, "어떻게"는 코드, "관리"는 `SkillManager`로 분리한다.

| 요소 | 역할 |
|------|------|
| `SkillManager` | AI마다 1개, 보유 스킬 + 쿨다운 관리, 동시에 여러 스킬 활성 가능 |
| `ISkill` / `SkillFactory` | `skill.csv`의 문자열 → `SkillId` enum 파싱 후 생성. `SkillId`는 `Dodge`/`Retreat`/`Heal` 3개뿐, 그 외는 경고 로그 후 null 반환 |
| `DodgeRollSkill` | 공격자/후퇴 방향으로 무적 구르기 |
| `RetreatSkill` | `RetreatPointSet` 지점 또는 타겟 반대 방향으로 이동, 중단돼도 목적지 캐시로 재개 |
| `HealSkill` | 지연 자가 회복, 사망 시 중단 |
| `SkillContext` | 스킬이 쓰는 참조 묶음 (Agent, Stat, Rotator, Animator, Target, Attacker) |
| `UseSkillAction` / `CanUseSkillCondition` | BT 브리지 노드 |
| `AIDodgeState` | 피격 시 회피 판정(`TryEvade`, 성공하면 데미지 무효) + 회피 의사 신호 |

수치 데이터는 `skill.csv` (범용 컬럼 speed/distance/duration/clip_name, 스킬마다 재해석). [data-tables.md](data-tables.md) 참고.

## 적 데이터 (`enemy.csv`)

| ID | 유형 |
|----|------|
| 1 | 일반 |
| 2 | 정예 |
| 3 | 보스 |

> **ID 마이그레이션 TODO:** 다른 테이블은 도메인별 1000단위 블록을 쓰는데 `enemy.csv`는 여전히 `1,2,3` — [datatable/id-ranges.md](../datatable/id-ranges.md).

## 전투 규칙

- 데미지·사망 판정: 호스트 전용 (`EnemyStat.TakeDamage`가 `RoomManager.IsHost` 체크)
- 게스트: AI/전투 컴포넌트 전부 비활성, 동기화된 위치·HP만 렌더
- 사망 시 `EnemyItemBoxDropper`가 필드 아이템 박스 생성, `RoomSync.EnemyDie`가 킬 지급 대상 플레이어 id를 함께 전송

## 관련 네트워크 패킷

전부 호스트→게스트 단방향(게스트→호스트 `G_Enemy*` 패킷 없음, 즉 게스트 측 예측 보정이 없어 호스트 랙이 그대로 노출됨):

`H_EnemySpawn`, `H_EnemyMove`, `H_EnemyHit`, `H_EnemyDie`, `H_EnemySpeak`, `H_EnemyShoot` — [multiplayer.md](multiplayer.md) 참고.

늦게 접속한 게스트는 `EnemyNetSync.SendAllToGuest` → `RoomSync.EnemySpawnToGuest`로 전체 적 스냅샷(타입/좌표/HP/장비)을 받는다.
