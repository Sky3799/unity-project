# 한국어 카드 배틀 게임 — Claude 작업 가이드

## 작업 원칙
1. **애매한 구현 사항은 반드시 먼저 질문하고 확인 후 작업할 것**
2. **폰트는 항상 네오 둥근모 적용** → `Assets/Fonts/NeoDunggeunmoPro-Regular (1) SDF.asset`
3. 기존에 작동 중인 기능은 건드리지 말 것
4. 씬 작업 전 반드시 현재 씬 Hierarchy 확인할 것

---

## 프로젝트 기본 정보
- **경로:** `C:/Users/hanel/My game project`
- **Unity 버전:** 6000.3.10f1 (Unity 6)
- **해상도:** 1280×720
- **Input System:** New Input System (UnityEngine.InputSystem) — 구 Input 클래스 사용 금지
- **렌더 파이프라인:** URP

---

## 씬 목록 및 흐름

```
TitleScene (0)
  → 클릭/키 입력 → MainMenuScene

MainMenuScene (1)  ⚠️ UI 개선 필요 (현재 단출함)
  → 스테이지 선택 → StageSelectScene
  → 수집 도감    → CollectionScene
  → 오답노트     → WrongNoteScene
  → 플레이 방법  → 팝업 (씬 이동 없음)

StageSelectScene (2)  ⚠️ UI 개선 필요 (맵 형식으로 개선 예정)
  → 스테이지 버튼 → BattleScene

BattleScene (3)
  → 클리어 시 3초 클리어 화면 → ResultScene
  → 패배 시 바로 → ResultScene
  → 나가기 버튼 → StageSelectScene

ResultScene (4)
  → 메인 화면으로 → MainMenuScene

WrongNoteScene (5)
CollectionScene (6)
```

---

## ⚠️ 미완성 / 개선 필요 항목
- **MainMenuScene**: 현재 버튼 4개만 있는 단출한 구성 → 퀄리티 개선 필요
- **StageSelectScene**: 단순 버튼 5개 나열 → 맵 형식(횡스크롤 노드 연결)으로 개선 예정
- **수집 도감 (CollectionScene)**: 1스테이지 호랑이 이미지만 있고 나머지는 텍스트
- **2~5스테이지**: 클리어 이미지 미작성 (`Assets/Resources/StageClear/Stage2~5Clear.png` 없음)
- **CollectionScene 몬스터 스프라이트**: `CollectionStorage.MonsterSpritePaths[]` 2~5번 비어있음
- **추후 스테이지 카드 및 적 캐릭터**: 2~5스테이지용 카드 데이터 및 적 캐릭터 미구현
- **주인공 공격/피격 모션**: 주인공 캐릭터 애니메이션 미제작
- **텍스트 크기 조절**: 전반적인 UI 텍스트 크기 검토 및 조정 필요
- **씬 전환 페이드 효과**: 현재 즉시 전환 → 부드러운 페이드인/아웃
- **피격/회복 숫자 팝업**: 데미지/힐 수치가 화면에 떠오르는 연출
- **화면 쉐이크**: 강타/오답 시 화면 진동 연출
- **오답 뱃지**: 오답노트에 쌓인 단어 있을 시 메인메뉴에서 뱃지 표시

---

## AI / 적응형 시스템 설계

### 출현 빈도 조절 (확정)
- 단어별 오답 횟수 누적 저장 (WrongAnswerStorage 확장)
- 오답 횟수에 따라 해당 카드 장수 증가 (덱 총 장수 가변)
  - 오답 0~1회 → 1장
  - 오답 2~3회 → 2장
  - 오답 4회+ → 3장
  - 숙달(연속 정답 3회+) → 1장 유지
- 회복/시간연장 카드는 기존 그대로 유지

### Gemini API 연동 (설계 확정, 카테고리 미정)
- **숙달 단어 다수 발생 시**: 해당 스테이지 카테고리의 심화 문제 생성 요청
- **스테이지 클리어 후**: 취약 단어 집중 문제 세트 생성
- 스테이지별 카테고리는 추후 확정 후 프롬프트 설계 진행
- 전투 중 API 호출 없음 (끊김 방지)

---

## 주요 에셋 경로
| 에셋 | 경로 |
|------|------|
| 폰트 | `Assets/Fonts/NeoDunggeunmoPro-Regular (1) SDF.asset` |
| 카드 프리팹 | `Assets/Prefabs/Card.prefab` |
| 카드 데이터 | `Assets/CardData/Card_*.asset` (10개) |
| 타이틀 배경 | `Assets/Sprites/Title/` |
| 버튼 스프라이트 | `Assets/Sprites/Button/startbutton.png` |
| 1스테이지 클리어 이미지 | `Assets/Resources/StageClear/Stage1Clear.png` |
| 호랑이 도감 이미지 | `Assets/Resources/숲속의 전투용 호랑이.png` |

---

## 스크립트 목록 (`Assets/Scripts/`)

### 전투 관련
| 스크립트 | 역할 |
|---------|------|
| `BattleManager.cs` | HP 관리, 셔플 덱(14장), 카드 사용, 퀴즈 결과 처리, 호랑이 애니메이션 연결 |
| `StageTimer.cs` | 전투 경과 시간 측정 |
| `StageClearOverlay.cs` | 클리어 시 3초 일러스트 + 텍스트 표시 (BattleScene GameCanvas에 붙어있음) |
| `ExitConfirmPopup.cs` | 나가기 버튼 → 확인 팝업 → StageSelectScene |

### 카드 관련
| 스크립트 | 역할 |
|---------|------|
| `Card/Card.cs` | 카드 UI 표시 (피해X, +15초, +20HP 등 타입별 표시) |
| `Card/CardData.cs` | 카드 ScriptableObject (CardType: Quiz/Heal/TimeExtend) |
| `Card/CardHand.cs` | 손패 시스템 (최대 5장, 부채꼴 정렬) |
| `Card/CardInteraction.cs` | 드래그/드롭 처리 |
| `Card/WordData.cs` | 퀴즈 단어 데이터 |

### 퀴즈 관련
| 스크립트 | 역할 |
|---------|------|
| `Quiz/QuizManager.cs` | 퀴즈 생성 (예문해석 형식) |
| `Quiz/QuizPopup.cs` | 3지선다 팝업 UI, 타이머, 결과 표시. 이벤트: `OnWrongAnswerRecorded`, `OnAnswerSelectedImmediate` |

### 씬 관련
| 스크립트 | 역할 |
|---------|------|
| `TitleSceneManager.cs` | 클릭/키 → MainMenuScene |
| `MainMenuManager.cs` | 4버튼 연결, HowToPlayPopup 관리 |
| `StageSelectManager.cs` | 5스테이지 버튼, PlayerPrefs 잠금/해제 |
| `ResultSceneManager.cs` | 결과 표시 (클리어/실패, 시간, 정답률 등) |
| `WrongNoteManager.cs` | 오답 목록 표시, 확인 버튼으로 개별 삭제 |
| `CollectionManager.cs` | 수집 도감 카드 표시 (340×460) |
| `BackToMainMenu.cs` | 뒤로 버튼 → MainMenuScene (미사용 가능) |

### 데이터 관련
| 스크립트 | 역할 |
|---------|------|
| `BattleResultData.cs` | 씬 간 전투 결과 전달 (static) |
| `WrongAnswerStorage.cs` | 오답 PlayerPrefs 저장/로드/삭제 |
| `CollectionStorage.cs` | 도감 데이터 (몬스터명, 칭호, 스프라이트 경로) |
| `StageClearSprites.cs` | Resources에서 클리어 이미지 로드 |

### 이펙트 관련
| 스크립트 | 역할 |
|---------|------|
| `BlinkText.cs` | TMP 알파 깜빡임 (TitleScene StartText에 사용) |
| `BlinkImage.cs` | Image 알파+스케일 펄스 |
| `TitleDecoEffect.cs` | (현재 미사용) |
| `GoldParticleEffect.cs` | (현재 미사용) |

---

## 셔플 덱 구성 (1스테이지)
- **문제카드 10장** + **체력회복 2장** + **시간연장 2장** = **총 14장**
- 14장 소진 시 다시 셔플

### 카드 레벨 및 데미지
| 레벨 | 카드 | 표시 | 데미지 |
|------|------|------|--------|
| Lv.1 | 금일, 당일, 명일, 익일, 본인 | 피해 20 | 20 |
| Lv.2 | 상기, 미결, 우천시, 하기, 공란 | 피해 25 | 25 |
| — | 시간연장 | 다음 문제 +5초 | — |
| — | 체력회복 | +20HP | — |

---

## PlayerPrefs 키 목록
| 키 | 용도 |
|----|------|
| `Stage{n}_Difficulty{d}_Cleared` | 스테이지 n 난이도 d 클리어 여부 (1=클리어) |
| `CurrentStage` | 현재 플레이 스테이지 번호 |
| `CurrentDifficulty` | 현재 플레이 난이도 (1/2/3) |
| `Collection_Stage{n}_Cleared` | 도감 클리어 여부 |
| `WrongAnswers_Stage{n}` | 오답 JSON 데이터 |

### 스테이지 해금 규칙
- Stage{n} 난이도 1/2/3 모두 클리어 → Stage{n+1} 난이도1 해금
- 난이도는 순서대로 해금 (이전 난이도 클리어 필수)

### 난이도별 수치
| 난이도 | 적 HP | 오답 시 내 피해 |
|--------|-------|----------------|
| 1 | 100 | 15 |
| 2 | 150 | 30 |
| 3 | 200 | 50 |

---

## BattleScene 구성
- `GameCanvas`: QuizManager, QuizPopup, StageClearOverlay, ExitConfirmPopup 컴포넌트 포함
- `BattleManager`: 별도 오브젝트
- `tiger_stand`: Animator (TigerController) — Attack/Hit 트리거 사용
- `tiger_attack`: 비활성 오브젝트 (Animator 컨트롤러 없음)

## 호랑이 애니메이션 연동
- 정답 → `SetTrigger("Hit")` × 3회 반복 (0.35초 간격)
- 오답 → `SetTrigger("Attack")` × 1회
- 팝업 닫힌 직후 발동, 1.2초 후 HP 반영 및 카드 드로우

---

## 주의사항
- DOTween 없음 → 모든 애니메이션은 코루틴
- `GameObject.Find()`는 비활성 오브젝트 탐색 불가 → `transform.Find()` 사용
- Screen Space Overlay Canvas에서 비활성 오브젝트는 Start()에서 잠깐 활성화 후 탐색
- `Image` + `TextMeshProUGUI` 동일 오브젝트에 붙이면 충돌 → 텍스트는 자식 오브젝트에 분리
- LayoutGroup 자식에 `LayoutElement` 없으면 크기 0으로 붕괴됨
