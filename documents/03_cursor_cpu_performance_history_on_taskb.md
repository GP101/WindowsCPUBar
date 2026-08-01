# Cpu performance history on taskbar
_Exported on 5/19/2026 at 10:30:11 GMT+9 from Cursor (3.4.20)_

---

**User**

taskbar의 버튼에 cpu performace를 표시하는데, 얼마간의 history를 보고 싶어. 어떻게 구현하는게 좋을까?

---

**Cursor**

프로젝트 구조와 CPU 표시 방식을 먼저 살펴보겠습니다.


코드를 보면 CPU는 `GetSystemTimes`로 샘플링하고, **작업 표시줄**에는 `ITaskbarList3.SetProgressValue`로 **현재 값 하나(0–100%)**만 넘기고 있습니다. 히스토리를 넣으려면 “어디에 그릴 수 있는지”와 “데이터를 어떻게 쌓을지”를 나눠서 설계하는 편이 좋습니다.

## 현재 구조의 한계

| 표시 위치 | 방식 | 히스토리 가능 여부 |
|-----------|------|-------------------|
| 작업 표시줄 버튼 | `TaskbarProgress.SetValue` | **불가** — 스칼라 진행률만 지원 |
| 제목 표시줄 | `TitleBarCpuRenderer` 커스텀 GDI | **가능** — 이미 막대를 직접 그림 |
| 창 안 ProgressBar | WinForms 기본 컨트롤 | **불가** — 커스텀 `Panel` 등으로 교체 필요 |

```202:214:d:\Work\TempWork\_ProgrammingProject\WindowsCpuBar\WindowsCpuBar\MainForm.cs
    private void OnTimerTick(object? sender, EventArgs e)
    {
        _cpuMonitor.Sample();
        var percent = (int)Math.Round(_cpuMonitor.UsagePercent);
        // ...
        TaskbarProgress.SetValue(Handle, percent);
```

작업 표시줄의 녹색/파란 진행 오버레이는 **한 시점의 퍼센트**만 표현합니다. API로 “과거 N개 구간”을 그릴 수는 없습니다.

---

## 추천 아키텍처 (프로젝트에 맞게)

### 1. `CpuHistoryBuffer` — 링 버퍼로 샘플 보관

`CpuMonitor`는 지금처럼 **현재 사용률**만 담당하고, 히스토리는 별도 클래스가 맡는 것이 깔끔합니다.

- 고정 크기 `double[]` + `writeIndex` + `count`
- `Add(percent)` — 타이머마다 `_cpuMonitor.UsagePercent` 푸시
- `CopyOrdered(Span<double>)` — **왼쪽=과거, 오른쪽=최신** 순으로 그리기용 복사

설정 예시 (`AppSettings`):

- `HistorySampleCount` (예: 60)
- 또는 `HistorySeconds` + `UpdateIntervalMs`로 샘플 수 계산  
  → 실제 구간 = `HistorySampleCount × UpdateIntervalMs` (1초 주기 × 60 = 약 1분)

갱신 주기를 바꿀 때는 버퍼 길이를 유지할지, “초 단위 윈도우”를 유지할지 정책 하나만 정하면 됩니다.

### 2. 그리기 — 스파크라인(막대 또는 꺾은선)

`TitleBarCpuRenderer`를 확장하거나 `CpuSparklineRenderer`를 공통으로 두는 방식이 자연스럽습니다.

- 영역을 `sampleCount`등분
- 각 칸 높이 = `percent / 100 × innerHeight` (막대형)
- 또는 `Graphics.DrawLines`로 꺾은선
- 0%는 바닥, 100%는 위 — **Y축은 고정 0–100**이 CPU에는 읽기 쉬움 (자동 스케일은 피크만 강조되어 일상 사용률이 잘 안 보임)

제목 표시줄은 이미 `TitleBarDrawWidth = 110` 픽셀이 있으므로, 예를 들어 30~60샘플이면 칸당 2~4px 막대로 “최근 30초~1분” 느낌을 줄 수 있습니다.

```5:40:d:\Work\TempWork\_ProgrammingProject\WindowsCpuBar\WindowsCpuBar\TitleBarCpuRenderer.cs
    public void Draw(
        Graphics graphics,
        Rectangle bounds,
        double currentPercent,
        ...
        var fillWidth = (int)Math.Round(inner.Width * currentPercent / 100.0);
        graphics.FillRectangle(fillBrush, inner.X, inner.Y, fillWidth, inner.Height);
```

지금은 **가로 한 줄 “현재값 막대”**이므로, 같은 `bounds` 안에서:

- **모드 A**: 전체를 스파크라인만
- **모드 B**: 왼쪽 80% 스파크라인 + 오른쪽 끝에 현재값 막대/숫자
- **모드 C**: 설정으로 전환

중 하나를 고르면 됩니다.

### 3. 작업 표시줄에 히스토리를 보이게 하려면

API 한계 때문에 **진행률 막대만으로는 히스토리 불가**입니다. 현실적인 선택지는 다음과 같습니다.

| 방법 | 장점 | 단점 |
|------|------|------|
| **A. 진행률 유지 + 제목/창에만 스파크라인** | 구현 단순, 기존 UX 유지 | 최소화 시 작업 표시줄에는 여전히 순간값만 |
| **B. `SetOverlayIcon`으로 작은 스파크라인 비트맵** | 최소화해도 버튼에 그래프 느낌 | 진행률과 겹칠 수 있음, 아이콘 리소스 해제 필요 |
| **C. 창 아이콘을 주기적으로 재생성** | 작업 표시줄 썸네일에 반영 | 해상도 작음, 아이콘 누수 주의 |
| **D. `SetThumbnailTooltip`에 텍스트 히스토리** | 구현 쉬움 | **호버할 때만** 보임 |

**추천 조합**

- 평소: 작업 표시줄 = **현재값** `SetProgressValue` (지금과 동일)
- 히스토리: **제목 표시줄 스파크라인** + (선택) 창 안 커스텀 패널
- 창이 최소화된 동안 작업 표시줄에도 꼭 그래프가 필요하면 **B(오버레이 아이콘)** 또는 진행률 대신 **B만** 사용

오버레이 아이콘은 대략 이런 흐름입니다 (구현 가이드용):

1. `Bitmap`(예: 16×16)에 투명 배경 + 스파크라인
2. `Icon.FromHandle` → `ITaskbarList3.SetOverlayIcon`
3. 이전 `HICON`/`Icon` dispose (누수 방지)
4. 진행률과 동시에 쓰면 시각적으로 복잡해질 수 있어 설정으로 on/off

### 4. 메인 창 ProgressBar

WinForms `ProgressBar`는 히스토리를 그릴 수 없습니다. `Panel` + `OnPaint`에서 `TitleBarCpuRenderer`와 같은 그리기 로직을 재사용하거나, 공통 `CpuSparklineRenderer.Draw(...)` 한 곳으로 모으면 됩니다.

### 5. 타이머 루프 변경 (개념)

```text
OnTimerTick:
  _cpuMonitor.Sample()
  _history.Add(_cpuMonitor.UsagePercent)
  TaskbarProgress.SetValue(...)           // 순간값
  _titleBarRenderer.Draw(..., _history)   // 스파크라인
  // optional: UpdateTaskbarOverlayIcon(_history)
```

---

## “얼마간”을 사용자에게 어떻게 보여줄지

- **샘플 개수** (`60 samples`) — 구현이 단순하고 메모리 고정
- **시간** (`60 seconds`) — UI에는 “최근 1분”처럼 표시하기 좋음  
  → `sampleCount = historySeconds * 1000 / updateIntervalMs`

첫 몇 틱은 `count < capacity`이므로 왼쪽을 비우거나, 채워진 구간만 그리면 됩니다.

---

## 정리

1. **`CpuHistoryBuffer`(링 버퍼)** 로 샘플 축적 — `CpuMonitor`와 분리  
2. **제목 표시줄(`TitleBarCpuRenderer`)** 에 스파크라인 — 비용 대비 효과가 가장 큼  
3. **작업 표시줄** 은 `SetProgressValue`만으로는 히스토리 불가 → 오버레이 아이콘/동적 아이콘/툴팁 중 선택  
4. 설정에 **샘플 수 또는 초 단위 윈도우** + (선택) 표시 위치 플래그  

원하시는 UX가 “최소화했을 때 작업 표시줄 버튼만으로 1분 그래프”인지, “제목 표시줄 + 작업 표시줄 진행률 병행”인지 알려주시면 그에 맞춰 모드(A/B/C)와 설정 항목을 더 구체적으로 짜 드리겠습니다. (Ask 모드라 코드 수정은 Agent 모드에서 해야 합니다.)

---

**User**

적용해서 수정해 줘.

---

**Cursor**

히스토리 버퍼와 스파크라인 렌더링을 추가하고, 제목 표시줄·메인 창·설정에 연결합니다.


히스토리 버퍼, 공통 스파크라인 렌더러, UI·작업 표시줄 연동을 구현합니다.


`MainForm.cs` 포맷을 정리하고 중복 `Resize` 핸들러를 합칩니다.


CPU 히스토리(스파크라인) 기능을 적용했습니다.

## 변경 요약

### 새 파일
- **`CpuHistoryBuffer`** — 링 버퍼로 샘플 저장 (오래된 값 → 최신 값 순)
- **`CpuSparklineRenderer`** — 막대형 스파크라인 공통 그리기 (오른쪽 정렬, 최신값 흰 테두리)
- **`CpuHistoryPanel`** — 메인 창용 커스텀 그래프 패널
- **`TaskbarOverlay`** — 최소화 시 작업 표시줄 오버레이 아이콘(16×16 스파크라인)

### 수정
- **`AppSettings`** — `HistorySeconds`(기본 60초), `GetHistoryCapacity()` 추가
- **`TitleBarCpuRenderer`** — 단일 막대 대신 히스토리 스파크라인
- **`MainForm`** — `ProgressBar` → `CpuHistoryPanel`, **히스토리 (초)** 설정 UI

### 표시 위치
| 위치 | 내용 |
|------|------|
| 메인 창 그래프 | 최근 N초 히스토리 |
| 제목 표시줄 (140px) | 동일 히스토리 |
| 작업 표시줄 진행률 | **현재 CPU %** (기존과 동일) |
| 작업 표시줄 오버레이 | **최소화 시**만 미니 스파크라인 |

히스토리 길이는 `히스토리(초) ÷ 갱신주기`로 샘플 수가 계산됩니다 (예: 60초, 1초 주기 → 60칸). 설정은 `%LocalAppData%\WindowsCpuBar\settings.json`에 저장됩니다.

빌드는 Release 기준으로 성공했습니다. 실행해 보시고, 오버레이를 항상 켜 두거나 진행률 대신 히스토리만 쓰고 싶으시면 말씀해 주세요.

---

**User**

최소화했을 때 taskbar의 버튼에도 history가 표시되게 할 수 있어?

---

**Cursor**

최소화 시 작업 표시줄 표시 구현을 확인하고, 히스토리가 더 잘 보이도록 개선할 수 있는지 살펴봅니다.


기존 16×16 오버레이는 작아서 잘 안 보입니다. 최소화 시 작업 표시줄 버튼 아이콘을 히스토리 스파크라인으로 동적으로 바꾸는 방식으로 개선합니다.


가능합니다. 이번에 그 방식으로 바꿔 두었습니다.

## 변경 내용

이전에는 작업 표시줄 **오른쪽 아래 작은 오버레이 배지**(16×16)에만 히스토리를 그려서 잘 안 보였습니다.

이제 **최소화 시 작업 표시줄 버튼 아이콘 자체**를 히스토리 스파크라인으로 바꿉니다 (`WM_SETICON`).

| 상태 | 작업 표시줄 버튼 |
|------|------------------|
| **최소화** | 16/32px 아이콘에 최근 CPU 히스토리 그래프 |
| **복원** | 원래 앱 아이콘으로 복구 |
| **항상** | 하단 **진행률 막대** = 현재 CPU % (기존과 동일) |

갱신 주기마다 아이콘이 다시 그려지므로, 최소화한 채로 두면 그래프가 계속 갱신됩니다.

앱을 다시 실행한 뒤 창을 최소화해 보시면, 작업 표시줄 버튼 아이콘에 막대 그래프가 보일 것입니다. 진행률 막대만 쓰고 아이콘은 고정하고 싶다면 그렇게도 조정할 수 있습니다.

