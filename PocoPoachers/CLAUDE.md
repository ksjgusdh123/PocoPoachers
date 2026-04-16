# CLAUDE.md

이 파일은 Claude Code(claude.ai/code)가 이 저장소에서 작업할 때 참고하는 가이드입니다.



\## 프로젝트 설명

Escape From Duckov게임을 모방하여 만듬

핵심 게임 루프

1. 준비: 로비에서 장비(총기, 가방)를 착용하고 맵에 진입.

2\. 탐색: 맵 곳곳에 배치된 아이템(빵, 통조림, 부품)을 파밍.

3\. 전투:  AI와 교전.

4\. 탈출: 최종 보스를 해치우고 지상을 탈환.

5\. 성장: 획득한 아이템을 판매하거나 창고에 저장, 더 좋은 장비 구매.



\## 지켜야할 규칙

* 최적화를 고려한 코드
* OOP 기반 설계
* 코드를 적용하기 전에 나에게 한번 물어 볼 것



\## 폴더 규칙



| 분류 | 경로 |

|-------|-------|

| Scripts | 'Assets/01. Scripts/{도메인}/' |

| Meshes | 'Assets/02. Meshes/{도메인}/' |

| Materials | 'Assets/03. Materials/{도메인}/' |

| Textures | 'Assets/04. Textures/{도메인}/' |

| Shaders | 'Assets/05. Shaders/{도메인}/' |

| Prefabs | 'Assets/06. Prefabs/{도메인}/' |

| ScriptableObjects | 'Assets/07. SO/{도메인}/' |

| Inputs | 'Assets/08. Inputs/' |

| SFXs | 'Assets/09. Sounds/SFX/{도메인}/' |

| BGMs | 'Assets/09. Sounds/BGM/' |

