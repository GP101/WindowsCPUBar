# GPU usage display and caption modification
_Exported on 5/26/2026 at 20:02:33 GMT+9 from Cursor (3.5.33)_

---

**User**

CPU바 밑에 같은 형식으로 GPU바도 출력하고 싶어. 그리고 최소화 했을 때, 아이콘 Update는 지금 그대로 두고, 캡션에는 CPU%/GPU%를 출력하고 싶어.

---

**Cursor**

Exploring the codebase to see how the CPU bar and minimized caption work, then drafting a plan for the GPU bar and caption changes.


Checking whether `CpuHistoryBuffer` is reusable and reviewing the project structure.


코드 구조를 파악했습니다. GPU 샘플링 방식과 UI 범위를 정리해 계획을 작성합니다.


**GPU 바 구현 확인**

> **GPU 스파크라인 색상은 CPU와 어떻게 할까요?**
> - CPU와 동일한 그래프 색상(설정 1개 공유)
> - GPU 전용 색상 추가(색상 선택 UI 별도) ✔

> **창이 복원(최대화/일반) 상태일 때, 제목 표시줄 오른쪽 스파크라인은?**
> - 지금처럼 CPU 히스토리만
> - CPU+GPU를 제목 표시줄에도 함께 표시 ✔

---

**User**

Implement the plan as specified, it is attached for your reference. Do NOT edit the plan file itself.

To-do's from the plan have already been created. Do not create them again. Mark them as in_progress as you work, starting with the first one. Don't stop until you have completed all the to-dos.

---

**Cursor**

Implementing the GPU bar and minimized caption per the plan. Starting with `GpuMonitor` and the PerformanceCounter package.


Updating MainForm with GPU UI and wiring.


GPU 바와 최소화 캡션을 계획대로 반영했습니다. Release 빌드는 성공했습니다.

### 추가·변경 사항

**`GpuMonitor.cs`** — Windows `GPU Engine` / `engtype_3D` Performance Counter로 어댑터별 최대값을 구한 뒤 전체 최대를 GPU %로 사용. 미지원 환경에서는 `IsAvailable = false`, `GPU: --%` 표시.

**`AppSettings.cs`** — `GpuBarColorArgb` (기본 녹색 `#10B981`) 및 `GpuBarColor` 프로퍼티.

**`MainForm.cs`**
- CPU 바 아래 GPU 스파크라인 패널·라벨·오버레이
- **CPU 그래프 색상...** / **GPU 그래프 색상...** 각각 미리보기
- 타이머에서 CPU/GPU 동시 샘플링·히스토리 갱신
- 작업 표시줄 진행률·최소화 아이콘: **CPU만** (기존과 동일)
- 최소화 캡션: `CPU n% GPU m%` (GPU 미지원 시 `GPU --%`)

**`TitleBarCpuRenderer.cs`** — 제목 표시줄 영역을 위(CPU)·아래(GPU)로 나눠 각각 스파크라인 표시.

**`WindowsCpuBar.csproj`** — `System.Diagnostics.PerformanceCounter` 8.0.0 패키지.

### 확인 방법

1. 앱 실행 후 CPU/GPU 바가 함께 갱신되는지 확인
2. 창 최소화 → 작업 표시줄 아이콘은 CPU 그래프만, 캡션은 `CPU n% GPU m%`
3. 창 복원 → 제목 표시줄 오른쪽에 CPU/GPU 스파크라인 2줄
4. GPU 색상 변경이 GPU 패널·제목 표시줄 하단에만 적용되는지 확인

GPU 카운터는 Windows 10 1709+ / WDDM 2.0+에서만 동작합니다. 원격 데스크톱·일부 VM에서는 GPU가 `--`로 표시될 수 있습니다.

