# 코드 제너레이터

Unity 에디터 메뉴에서 CSV·FlatBuffer 스키마를 C#/JSON으로 변환하는 도구.

경로: `Assets/01. Scripts/Core/Editor/`

---

## 메뉴 요약

| 메뉴 | 클래스 | 출력 |
|------|--------|------|
| **Tools → Generator → Tables** | `TableGeneratorTool` | DataTable C# + JSON |
| **Tools → Generator → Packets** | `PacketGenerator` | FlatBuffer C# + 핸들러 스텁 |

---

## Table Generator

### 입력·출력

| | 경로 |
|--|------|
| 입력 CSV | `PocoPoachers/DataTable/*.csv` |
| 출력 C# | `Assets/01. Scripts/Generated/DataTable/` |
| 출력 JSON | `Assets/_Data/Resources/JsonData/` |

`DataTable/` 없으면 `Data/` 폴더 폴백.

### 동작

1. CSV 헤더에서 타입 추론 (`int`, `float`, `string`, `bool`, enum)
2. `_memo` 등 디자이너 전용 컬럼 제거 (`StripDesignerColumns`)
3. enum 컬럼이면 `{TableName}{Column}Type` enum 생성
4. `{Name}Data` 레코드 + `{Name}Table` 싱글톤 생성
5. JSON 직렬화 후 `Resources/JsonData/{name}.json` 저장

### CSV 수정 후 워크플로

```
1. PocoPoachers/DataTable/*.csv 편집
2. Unity → Tools → Generator → Tables
3. 런타임: DataManager가 JsonData 로드 → *Table.Instance
```

ID 범위 규칙: [datatable/id-ranges.md](../datatable/id-ranges.md)

### 새 테이블 추가

1. `DataTable/my_table.csv` 생성 (첫 행 헤더, `_` 시작 컬럼은 제외 가능)
2. Generator 실행
3. `DataManager` 등에서 `MyTable.Instance` 사용처 연결
4. [data-tables.md](../design/data-tables.md) 문서 갱신

---

## Packet Generator

### 입력·출력

| | 경로 |
|--|------|
| flatc | `PocoPoachers/FlatBuffer/flatc.exe` |
| 스키마 | `PocoPoachers/FlatBuffer/Schemas/**/*.fbs` |
| 클라 출력 | `Assets/01. Scripts/Generated/FlatBuffer/` |
| 서버 출력 | `Server/Server/Generated/FlatBuffer/` |
| 클라 핸들러 | `Network/Packet/{G,H,SPacketHandlers}/` |
| 서버 핸들러 | `Server/Server/Net/Packet/PacketHandler/` |

### 동작

1. `flatc --csharp` 로 각 `.fbs` → C# 생성 (클라+서버)
2. `Main.fbs` 파싱 → `C_` / `S_`+`G_`+`H_` 패킷 목록 분리
3. `PacketManager.Generated.cs` 생성 (수신 등록)
4. 미구현 핸들러 스텁 생성 (`PacketHandler.{Schema}.cs` 또는 `.temp.cs`)

### 접두사별 핸들러 폴더

| 접두사 | 클라이언트 | 서버 |
|--------|------------|------|
| `C_` | — | `PacketHandler/` |
| `S_` | `SPacketHandlers/` | — |
| `G_` | `GPacketHandlers/` | — |
| `H_` | `HPacketHandlers/` | — |

### 패킷 추가 후 워크플로

```
1. FlatBuffer/Schemas/Game/*.fbs 에 table 추가
2. Main.fbs union PacketType 에 등록
3. Unity → Tools → Generator → Packets
4. 생성된 .temp.cs 확인 → 핸들러 구현
5. Server 솔루션 빌드 (서버 C#도 재생성됨)
6. 클라 RoomSync / PacketHandlers 구현
```

상세 패킷 규칙: [network-packets.md](network-packets.md)

### 스텁 vs 기존 핸들러

- `CollectImplementedMethods`로 이미 `OnXxx`가 있으면 스킵
- 없으면 `// TODO` 스텁 생성
- 같은 스키마 파일에 패킷 추가 시 기존 `PacketHandler.{Schema}.cs`가 있으면 `.temp.cs`로 출력 → **수동 병합 필요**

---

## Includer (`Includer.cs`)

Unity csproj 생성 시 Editor 프로젝트에 외부 파일 포함:

- `FlatBuffer/**/*.fbs`
- `DataTable/**/*.csv`

IDE에서 스키마·CSV를 솔루션 탐색기에 표시. 빌드에는 영향 없음.

---

## Icon Generator (참고)

`IconGenerator.cs` — 프리팹에서 아이콘 PNG 추출 (테이블 `icon` 필드용).  
메뉴 경로는 `TableGenerator`와 동일 `Tools/Generator/` 하위.

---

## 주의사항

| 항목 | 설명 |
|------|------|
| Generated 폴더 | **수동 편집 금지** — 재생성 시 덮어씀 |
| 핸들러 | `PacketHandlers` partial class — **수동 유지**, 스텁만 자동 |
| 서버 동기화 | 패킷 변경 시 클라+서버 양쪽 Generator 실행 |
| flatc.exe | Windows 전용 경로. 다른 OS는 flatc 경로 수정 필요 |
| JSON 경로 | `Resources/JsonData` — 런타임 `Resources.Load` |

---

## 체크리스트 (데이터+패킷 동시 변경 시)

- [ ] CSV 수정 → Tables 생성
- [ ] fbs 수정 → Packets 생성
- [ ] 핸들러 `.temp.cs` 병합
- [ ] `RoomSync` 송신 추가
- [ ] 서버 `Server.sln` 빌드
- [ ] Unity 컴파일 확인
- [ ] [id-ranges.md](../datatable/id-ranges.md) / [data-tables.md](../design/data-tables.md) 문서 갱신
