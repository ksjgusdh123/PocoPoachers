"""
Map Generator용 커스텀 Height Map 생성.
콘셉트: 원형 산맥으로 둘러싸인 분지 지형. 분지 안쪽에는 5개의 평평한
세부지역 기반(plateau)을 두고, 최소 스패닝 트리로 도로(평탄화된 띠)를 연결한다.
지역/도로는 주변 지형보다 낮게 파여 있고, 산맥의 영향 범위와는 절대 겹치지 않는다.

산출물: Assets/_Art/Map/height_map_custom.png (512x512, grayscale)
기존 샘플 height_map.png(generate_layers.py 산출물)는 건드리지 않는다.
"""

import math

import numpy as np
from PIL import Image

SIZE = 512
OUT_PATH = "../../Assets/_Art/Map/height_map_custom.png"

rng = np.random.default_rng(7)

# ---------------------------------------------------------------
# 좌표계: 중심 (0,0), 이미지 짧은 변 절반을 반지름 1로 정규화
# ---------------------------------------------------------------
ys, xs = np.mgrid[0:SIZE, 0:SIZE]
nx = (xs - SIZE / 2) / (SIZE / 2)
ny = (ys - SIZE / 2) / (SIZE / 2)
r = np.sqrt(nx ** 2 + ny ** 2)
theta = np.arctan2(ny, nx)


def value_noise(size, scale, octaves=4, persistence=0.5, seed=0):
    r_ = np.random.default_rng(seed)
    total = np.zeros((size, size), dtype=np.float64)
    amp = 1.0
    amp_sum = 0.0
    for o in range(octaves):
        cell = max(2, scale // (2 ** o))
        grid_n = size // cell + 2
        grid = r_.random((grid_n, grid_n)).astype(np.float32)
        layer = np.asarray(
            Image.fromarray(grid, mode="F").resize((size, size), Image.BICUBIC),
            dtype=np.float64,
        )
        total += layer * amp
        amp_sum += amp
        amp *= persistence
    total /= amp_sum
    return total


def smoothstep(edge0, edge1, x):
    t = np.clip((x - edge0) / (edge1 - edge0), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def point_segment_dist(px, py, ax, ay, bx, by):
    """격자 전체(px,py)에서 선분 A-B까지의 최단 거리."""
    abx, aby = bx - ax, by - ay
    ab_len2 = abx * abx + aby * aby
    t = ((px - ax) * abx + (py - ay) * aby) / ab_len2
    t = np.clip(t, 0.0, 1.0)
    cx = ax + t * abx
    cy = ay + t * aby
    return np.sqrt((px - cx) ** 2 + (py - cy) ** 2)


# ---------------------------------------------------------------
# 1) 기반 지형: 낮고 완만한 분지 바닥 (약한 노이즈)
# ---------------------------------------------------------------
BASE_LEVEL = 0.30
NOISE_AMP = 0.05
base_noise = value_noise(SIZE, 40, octaves=4, persistence=0.5, seed=10)
base_noise = (base_noise - base_noise.min()) / (base_noise.max() - base_noise.min())
terrain = BASE_LEVEL + (base_noise - 0.5) * 2 * NOISE_AMP

# ---------------------------------------------------------------
# 2) 원형 산맥 링: 반지름이 각도에 따라 흔들리는 넓은 가우시안 능선
# ---------------------------------------------------------------
RING_R0 = 0.80
RING_WIDTH = 0.14          # 넓을수록 산맥 띠가 두꺼워짐
RING_SAFE_SIGMA = 1.6      # 이 시그마 밖은 "산맥 영향 없음"으로 간주 (지역 배치용)
MOUNTAIN_PEAK = 0.95

harmonics = [(2, 0.05), (3, 0.03), (5, 0.02)]
phases = rng.uniform(0, 2 * math.pi, size=len(harmonics))


def ring_radius_at(angle):
    """지정 각도에서 산맥 능선의 중심 반지름 (grid용 ring_radius_theta와 동일 공식)."""
    rr = RING_R0
    for (freq, amp), phase in zip(harmonics, phases):
        rr += amp * np.sin(freq * angle + phase)
    return rr


def ring_inner_safe_radius(angle):
    """이 반지름보다 안쪽이면 산맥 가우시안이 사실상 0 — 지역이 침범해도 안전."""
    return ring_radius_at(angle) - RING_SAFE_SIGMA * RING_WIDTH


ring_radius_theta = ring_radius_at(theta)

ring_texture = value_noise(SIZE, 20, octaves=3, persistence=0.5, seed=11)
ring_texture = (ring_texture - ring_texture.min()) / (ring_texture.max() - ring_texture.min())

ring_gauss = np.exp(-((r - ring_radius_theta) ** 2) / (2 * RING_WIDTH ** 2))
ring_gauss *= 0.55 + 0.55 * ring_texture  # 능선 굵기/높이에 자연스러운 편차
ring_gauss = np.clip(ring_gauss, 0.0, 1.0)

mountain = BASE_LEVEL + (MOUNTAIN_PEAK - BASE_LEVEL) * ring_gauss
terrain = np.maximum(terrain, mountain)

# ---------------------------------------------------------------
# 3) 안쪽 세부지역 5곳: 평평한 원형 기반. 산맥보다 "낮게" 파인 지형이며,
#    산맥의 안전 반지름(ring_inner_safe_radius) 밖으로 절대 나가지 않도록
#    각도별로 배치 가능한 최대 반지름을 계산해 그 안에서만 샘플링한다.
# ---------------------------------------------------------------
REGION_COUNT = 5
PLATEAU_RADIUS_RANGE = (0.09, 0.12)   # 지역마다 크기도 다르게
PLATEAU_FALLOFF = 0.04                # 바깥 지형과 섞이는 폭
PLATEAU_HEIGHT = 0.16                 # BASE_LEVEL(0.30)보다 낮음 — 주변보다 파인 기반
REGION_GAP = 0.015                    # 지역끼리도 서로 겹치지 않도록 여유
RING_MARGIN = 0.02                    # 산맥 안전 반지름에서 한 번 더 여유
MIN_REGION_R = 0.16                   # 지역이 중심에 너무 가깝게 몰리지 않도록 최소 거리
                                       # (안 그러면 중심 근처를 막아 다른 지역이 들어갈 자리가 없어짐)

region_centers = []
region_radii = []
attempts = 0
while len(region_centers) < REGION_COUNT and attempts < 20000:
    attempts += 1
    i = len(region_centers)
    base_angle = -math.pi / 2 + i * (2 * math.pi / REGION_COUNT)
    a = base_angle + rng.uniform(-0.6, 0.6)          # 약 ±34도 랜덤
    plateau_r = rng.uniform(*PLATEAU_RADIUS_RANGE)
    reach = plateau_r + PLATEAU_FALLOFF

    max_rr = ring_inner_safe_radius(a) - RING_MARGIN - reach
    if max_rr < MIN_REGION_R:
        continue  # 이 각도는 산맥이 너무 안쪽까지 들어와 있음 — 다시 시도
    rr = rng.uniform(MIN_REGION_R, max_rr)
    cx, cy = rr * math.cos(a), rr * math.sin(a)

    too_close = any(
        math.hypot(cx - ex, cy - ey) < reach + er + REGION_GAP
        for (ex, ey), er in zip(region_centers, region_radii)
    )
    if too_close:
        continue
    region_centers.append((cx, cy))
    region_radii.append(plateau_r)

if len(region_centers) < REGION_COUNT:
    raise RuntimeError(f"지역을 {REGION_COUNT}개 배치하지 못함 ({len(region_centers)}개만 성공) — 파라미터를 완화하세요.")

region_influence = np.zeros_like(terrain)
for (cx, cy), plateau_r in zip(region_centers, region_radii):
    d = np.sqrt((nx - cx) ** 2 + (ny - cy) ** 2)
    infl = 1.0 - smoothstep(plateau_r, plateau_r + PLATEAU_FALLOFF, d)
    region_influence = np.maximum(region_influence, infl)

terrain = terrain * (1 - region_influence) + PLATEAU_HEIGHT * region_influence

# ---------------------------------------------------------------
# 4) 지역 간 도로: 지정된 분기 구조로 연결
#    1-2번 지역은 그대로, 2번 지역에서 3/4번 지역으로 갈라지고, 4번 지역에서 5번 지역으로 이어짐
# ---------------------------------------------------------------
ROAD_WIDTH = 0.035
ROAD_FALLOFF = 0.02
ROAD_HEIGHT = PLATEAU_HEIGHT

edges = [(0, 1), (1, 2), (1, 3), (3, 4)]  # (region0=1번, region1=2번, ... region4=5번)

road_influence = np.zeros_like(terrain)
for i, j in edges:
    ax, ay = region_centers[i]
    bx, by = region_centers[j]
    dist = point_segment_dist(nx, ny, ax, ay, bx, by)
    infl = 1.0 - smoothstep(ROAD_WIDTH, ROAD_WIDTH + ROAD_FALLOFF, dist)
    road_influence = np.maximum(road_influence, infl)

terrain = terrain * (1 - road_influence) + ROAD_HEIGHT * road_influence

# ---------------------------------------------------------------
# 저장
# ---------------------------------------------------------------
terrain = np.clip(terrain, 0.0, 1.0)
height_img = (terrain * 255).astype(np.uint8)
Image.fromarray(height_img, mode="L").convert("RGB").save(OUT_PATH)

print("saved", OUT_PATH)
print("region centers (normalized xy, -1..1):")
for i, (cx, cy) in enumerate(region_centers):
    print(f"  region {i}: ({cx:.3f}, {cy:.3f})  px=({SIZE/2 + cx*SIZE/2:.0f}, {SIZE/2 + cy*SIZE/2:.0f})")
