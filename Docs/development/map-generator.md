# 맵 자동 생성 (Map Generator)

이미지 레이어(Height/Biome/Object/Resource/Enemy Spawn Map)를 읽어 Terrain + 프리팹 배치 + NavMesh를 자동 생성하는 에디터 툴.
기획 배경: [design/map-generation.md](../design/map-generation.md)

메뉴: **Tools → Generator → Map**

---

## 파일 위치

| 파일 | 역할 |
|------|------|
| `Assets/01. Scripts/Core/Editor/MapGeneratorWindow.cs` | 에디터 윈도우 + 생성 파이프라인 |
| `Assets/01. Scripts/Core/MapGen/MapLayerPalette.cs` | 색상↔프리팹/TerrainLayer 매핑 ScriptableObject |
| `Assets/_Data/Map/LayerPalette/MapLayerPalette.asset` | 현재 사용 중인 팔레트 에셋 |
| `Assets/_Data/Map/TerrainLayers/*.terrainlayer` | 바이옴별 TerrainLayer (단색 틴트, 텍스처 아트 없음) |
| `Assets/_Art/Environment/Textures/White.png` | TerrainLayer가 공유하는 흰색 텍스처 (DiffuseRemap으로 색 틴트) |
| `Assets/02. Prefabs/Environment/*.prefab` | 아트 없는 더미 프리팹 (Tree/Building/DungeonEntrance/PlantResource/RareResource) |
| `Assets/_Generated/MapGen/` | 생성될 때마다 갱신되는 산출물 (TerrainData, Terrain 머티리얼) — **수동 편집 금지** |
| `Assets/_Art/Map/*.png` | 테스트용 샘플 레이어 이미지 (5종) |
| `Tools/MapGen/generate_layers.py` | 위 샘플 이미지를 재생성하는 Python 스크립트 (Unity 밖, `DataTable/`·`FlatBuffer/`와 동급의 도구 폴더) |

---

## 워크플로

```
1. 레이어 PNG 준비 (Height/Biome/Object/Resource/Enemy Spawn Map, 동일 좌표계)
2. 각 텍스처 Import Settings: Read/Write Enabled 켜기, Compression None
3. MapLayerPalette 에셋에 색상↔TerrainLayer/프리팹 매핑 채우기 (아래 표 참고)
4. Tools → Generator → Map 에서 5개 텍스처 + 팔레트 연결, 지형 크기 설정
5. "맵 생성" 클릭 → 완료 다이얼로그 확인
6. 재수정 시: 이미지만 바꾸고 다시 "맵 생성" (기존 GeneratedMap 삭제 확인창 뜸)
```

---

## 색상 규칙 (현재 팔레트 기준)

### Biome Map → TerrainLayer

| Hex | 이름 |
|-----|------|
| `348F3C` | Forest |
| `DEC83C` | Grassland |
| `8C603A` | Wasteland |
| `F5F5F5` | Snow |
| `366CD6` | Water |

### Object Map → 프리팹

| Hex | 이름 | 연결된 프리팹 |
|-----|------|----------------|
| `EBD228` | Building | `Building_Dummy` |
| `969696` | Rock | `SandRubble` (기존 Obstacle) |
| `E62828` | Tree | `Tree_Dummy` |
| `9628C8` | DungeonEntrance | `DungeonEntrance_Dummy` |

### Resource Map → 프리팹

| Hex | 이름 | 연결된 프리팹 |
|-----|------|----------------|
| `3CC85A` | PlantResource | `PlantResource_Dummy` |
| `F0DC32` | RareResource | `RareResource_Dummy` |
| `3C82E6` | MineralResource | `TestOre` (기존 Ore) |

### Enemy Spawn Map

흰색 픽셀(밝기 ≥ 임계값, 기본 0.5) = 스폰 후보 → NavMesh 위 유효 지점만 최종 채택.

> 더미 프리팹은 아트가 없어 임시로 만든 도형(원기둥/큐브/구)이다. 실제 아트 완성되면 팔레트에서 프리팹만 교체하면 됨.

---

## 팔레트(MapLayerPalette) 필드 채울 때 주의

**기본값(0)인 채로 두면 조용히 아무것도 배치되지 않는다** — 에러 없이 그냥 빈 결과가 나오므로 반드시 확인:

| 필드 | 기본값 | 두면 안 되는 이유 |
|------|--------|---------------------|
| `color` | `(0,0,0,0)` | Object/Resource Map 배경이 검정이라, 안 채우면 배경 전체가 매칭되어 마지막 항목이 지형 전체에 도배됨 |
| `density` | `0` | `Random.value > density`가 항상 참이 되어 **거의 배치 안 됨** |
| `uniformScaleRange` | `{0,0}` | 배치돼도 스케일 0 = 투명하게 안 보임 |

---

## 알려진 제한 사항

| 항목 | 설명 |
|------|------|
| Road Map 레이어 | 제안서에는 있으나 샘플 이미지·구현 모두 없음 |
| NavMesh 장애물 처리 | 배치된 오브젝트(나무 등)에 `NavMeshModifier`를 자동으로 붙이지 않음 — NavMesh가 오브젝트를 뚫고 지나감 |
| Biome 미설정 시 | `biomes` 배열이 비어있으면 지형 텍스처링을 조용히 스킵 (Unity 기본 텍스처로 나옴, 에러 아님) |
| TerrainLayer 텍스처 | Wrap Mode가 Repeat가 아니면 Unity가 경고 + 지형이 핑크로 렌더링됨 |

---

## 트러블슈팅

| 증상 | 원인 | 조치 |
|------|------|------|
| "맵 생성" 버튼 비활성화 | 레이어 텍스처 Read/Write Enabled 꺼짐 | Import Settings → Advanced → Read/Write Enabled 켜고 Apply |
| 지형이 핑크 | ① TerrainLayer 텍스처 Wrap Mode가 Clamp<br>② (해결됨) 스크립트로 만든 Terrain은 URP 머티리얼이 비어있음 | ① 텍스처 Wrap Mode를 Repeat로<br>② 코드에서 `GetOrCreateTerrainMaterial()`로 자동 할당하도록 이미 수정됨 — 최신 스크립트인지 확인 |
| 바이옴 색이 안 칠해짐 | (해결됨) `TerrainData.alphamapResolution` 기본값이 0이라 페인팅 루프가 0회 실행됨 | 코드에서 512로 강제 설정하도록 이미 수정됨 |
| Object/Resource가 하나도 안 보임 | 팔레트의 `color`/`density`/`uniformScaleRange`가 기본값 | 위 "팔레트 필드 채울 때 주의" 표 참고 |

---

## 향후 확장 아이디어

- Road Map 레이어 추가 (경로/이동로 생성)
- 오브젝트 배치 시 `NavMeshModifier` 자동 부착 (장애물 회피)
- 더미 프리팹 → 실제 아트 프리팹 교체
- PSD 레이어 직접 Import, AI 기반 레이어 이미지 생성 연동 (제안서 9장 참고)
