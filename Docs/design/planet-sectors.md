# 행성 대역별 스펙 (기획)

4개 행성(레이드 맵) Tier별 진입 조건·환경·자원·위험도 설계 기준.

구현 현황(`planet.csv`)은 [shelter-raid.md](shelter-raid.md#행성-데이터-planetcsv), 미연동 필드는 [data-tables.md](data-tables.md#미연동-데이터) 참고.

---

## 스펙 표

| 섹터 | 진입 제한 | 안개 밀도 / 가시거리 | 수직성 테마 | 광물 매장 티어 | 위험도 / 스폰 캡 |
|------|-----------|----------------------|-------------|-----------------|-------------------|
| **섹터 01: 폐기 황무지** (Tier 1) | 기본 해금 (비용 없음) | 0.01 / 150m | 오픈형 야외 사막, 평면 고철장 | 철·구리·석탄 (T1) | 최하 (최대 12) |
| **섹터 02: 동결 정점** (Tier 2) | 벙커 Lv.2 + 전력 100 | 0.15 / 60m | 복층형 빙하 절벽, 미로형 얼음 동굴 | 티타늄·블루 크리스탈 | 중 (최대 20) |
| **섹터 03: 화산 원자로** (Tier 3) | 벙커 Lv.3 + 전력 250 | 0.25 / 40m | 고저차 심한 용암 대지, 폐 기업 공장 | 우라늄·레드 플라즈마 | 상 (최대 28) |
| **섹터 04: 미지의 심연** (Tier 4) | 벙커 Lv.4 + 보스 코어 1개 소비 | 0.60 / 10m (극단 제한) | 완전 암전 지대, 고대 3D 입체 미로 유적 | 오메가 광물 (T4 집중) | 최상 (**야간 시 스폰 캡 해제**) |

---

## 컨셉 이미지 (무드보드)

절차적으로 생성한 색감·분위기 참고용 이미지. 실제 아트 디렉션용은 아님 — 안개 밀도·가시거리·수직성 테마를 색감/구도로 대략 표현한 것.

| 섹터 | 이미지 |
|------|--------|
| 01: 폐기 황무지 | [img/planet-sectors/sector01_wasteland.png](img/planet-sectors/sector01_wasteland.png) |
| 02: 동결 정점 | [img/planet-sectors/sector02_frozen_summit.png](img/planet-sectors/sector02_frozen_summit.png) |
| 03: 화산 원자로 | [img/planet-sectors/sector03_volcanic_reactor.png](img/planet-sectors/sector03_volcanic_reactor.png) |
| 04: 미지의 심연 | [img/planet-sectors/sector04_unknown_abyss.png](img/planet-sectors/sector04_unknown_abyss.png) |

생성 스크립트: `PocoPoachers/Tools/MapGen/generate_sectors.py`

---

## 구현 갭

| 섹터 | `planet.csv` 등록 | 비고 |
|------|---------------------|------|
| 섹터 01~03 | ✅ (id 1001~1003) | [shelter-raid.md](shelter-raid.md#행성-데이터-planetcsv) 참고 |
| 섹터 04: 미지의 심연 | ❌ 미등록 | Tier 4, 보스 코어 소비 조건 포함 신규 추가 필요 |

`planet.csv`에는 `need_power`/`use_time_limit`/`max_session_time`/`fog_density`/`draw_distance` 필드가 이미 있지만 **선택 UI 잠금(shelter_level)에만 사용되고 레이드 런타임에는 미적용** — 상세: [todo.md](todo.md#행성-런타임-규칙-planetcsv).

아래 항목은 `planet.csv`에 대응 컬럼이 아예 없어 **설계 의도로만 존재**:

| 스펙 항목 | 비고 |
|-----------|------|
| 수직성 테마 (Verticality) | 맵 지형 설계 가이드 — [맵 자동 생성](map-generation.md)의 Height Map으로 표현 가능 |
| 광물 매장 티어 가치 | `mineral.csv`와 티어 연동 안 됨 |
| 위험도 / 스폰 캡 | `EnemySpawner`에 캡·야간 해제 로직 없음 (항상 고정 스폰) |

---

## 관련 문서

- [shelter-raid.md](shelter-raid.md) — 행성 잠금·레이드 흐름 구현
- [data-tables.md](data-tables.md) — `planet.csv` 필드 정의
- [map-generation.md](map-generation.md) — 안개/가시거리/지형을 실제 맵으로 만드는 파이프라인
- [todo.md](todo.md) — 행성 런타임 규칙 미구현 목록
