# 행성 대역별 스펙 (기획)

4개 행성(레이드 맵) Tier별 진입 조건·환경·자원·위험도 **설계 목표치**. 이 문서의 "섹터" 개념은 기획 의도이며, 코드에는 `sector`/`danger` 관련 클래스나 필드가 전혀 존재하지 않는다 — 실제로는 행성 하나 = 씬 하나의 평면 구조다.

구현 현황은 [shelter-raid.md](shelter-raid.md#행성-데이터-planetcsv), 데이터 필드는 [data-tables.md](data-tables.md) 참고.

---

## 스펙 표 (설계 목표 — 런타임 미적용)

| 섹터 | 진입 제한 | 안개 밀도 / 가시거리 | 수직성 테마 | 광물 매장 티어 | 위험도 / 스폰 캡 |
|------|-----------|----------------------|-------------|-----------------|-------------------|
| **섹터 01: 폐기 황무지** (Tier 1) | 기본 해금 (비용 없음) | 0.01 / 150m | 오픈형 야외 사막, 평면 고철장 | 철·구리·석탄 (T1) | 최하 (최대 12) |
| **섹터 02: 동결 정점** (Tier 2) | 벙커 Lv.2 + 전력 100 | 0.15 / 60m | 복층형 빙하 절벽, 미로형 얼음 동굴 | 티타늄·블루 크리스탈 | 중 (최대 20) |
| **섹터 03: 화산 원자로** (Tier 3) | 벙커 Lv.3 + 전력 250 | 0.25 / 40m | 고저차 심한 용암 대지, 폐 기업 공장 | 우라늄·레드 플라즈마 | 상 (최대 28) |
| **섹터 04: 미지의 심연** (Tier 4) | 벙커 Lv.4 + 보스 코어 1개 소비 | 0.60 / 10m (극단 제한) | 완전 암전 지대, 고대 3D 입체 미로 유적 | 오메가 광물 (T4 집중) | 최상 (**야간 시 스폰 캡 해제**) |

---

## 컨셉 이미지 (무드보드)

| 섹터 | 이미지 |
|------|--------|
| 01: 폐기 황무지 | [img/planet-sectors/sector01_wasteland.png](img/planet-sectors/sector01_wasteland.png) |
| 02: 동결 정점 | [img/planet-sectors/sector02_frozen_summit.png](img/planet-sectors/sector02_frozen_summit.png) |
| 03: 화산 원자로 | [img/planet-sectors/sector03_volcanic_reactor.png](img/planet-sectors/sector03_volcanic_reactor.png) |
| 04: 미지의 심연 | [img/planet-sectors/sector04_unknown_abyss.png](img/planet-sectors/sector04_unknown_abyss.png) |

생성 스크립트: `PocoPoachers/Tools/MapGen/generate_sectors.py`

---

## 실제 구현 상태 (코드 확인)

`planet.csv`에는 `id, planet_name, tier, need_shelter_level, need_power, use_time_limit, max_session_time, fog_density, draw_distance, icon` 컬럼이 있지만, 저장소 전체 검색 결과 **런타임에 실제로 읽히는 필드는 `id`, `planet_name`, `need_shelter_level`, `icon` 4개뿐**이다.

| 필드 | 사용처 | 상태 |
|------|--------|------|
| `id` | 씬 이름 `SC_Raid_{id}` 조합, `GameManager.SetSelectedPlanet` | ✅ 사용 |
| `planet_name` | `PlanetSlotUI` 로컬라이즈 표시 | ✅ 사용 |
| `need_shelter_level` | `ShelterManager.IsPlanetUnlocked` — 선택 UI 잠금의 **유일한** 게이트 | ✅ 사용 |
| `icon` | 행성 선택 슬롯 아이콘 | ✅ 사용 |
| `tier` | — | ❌ 아무 코드도 참조 안 함 |
| `need_power` | — | ❌ 전력 요구 검증 로직 없음 |
| `use_time_limit` / `max_session_time` | — | ❌ 레이드 세션 제한 시간 로직 없음. `RaidStats`는 경과시간을 표시만 하고 상한 비교 안 함 |
| `fog_density` / `draw_distance` | — | ❌ 안개/시야는 행성과 무관한 고정 `VisionConfig` 애셋 하나로 전 씬 동일하게 적용됨 |

**섹터 04(미지의 심연)는 `planet.csv`에 아예 등록되어 있지 않다.**

아래 항목은 대응 컬럼조차 없어 순수 설계 의도로만 존재:

| 스펙 항목 | 비고 |
|-----------|------|
| 수직성 테마 (Verticality) | [맵 자동 생성](map-generation.md)의 Height Map으로 표현 가능하나 행성별로 다른 지형을 자동 연결하는 로직 없음 |
| 광물 매장 티어 가치 | `mineral.csv`에 행성/티어 연동 컬럼 없음 |
| 위험도 / 스폰 캡 | `EnemySpawner`는 씬 시작 시 고정 배열을 1회 스폰할 뿐 — 캡·주야 조건 없음 |

---

## 관련 문서

- [shelter-raid.md](shelter-raid.md) — 행성 잠금·레이드 흐름 구현
- [data-tables.md](data-tables.md) — `planet.csv` 필드 정의
- [map-generation.md](map-generation.md) — 안개/가시거리/지형을 실제 맵으로 만드는 파이프라인 (현재 런타임 미연동)
- [todo.md](todo.md) — 행성 런타임 규칙 미구현 목록
