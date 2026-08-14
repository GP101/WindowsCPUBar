# 변경 사항 요약 (Walkthrough)

CPU/GPU 점유율을 표시하는 프로그램 자체가 CPU를 과도하게 점유하지 않도록, 매 타이머 틱(기본 1초, 최소 200ms)마다 실행되는 `MainForm.OnTimerTick` 경로를 점검하고 불필요한 부하를 제거했습니다.

## 점검 결과 (발견한 문제)

### 1. 작업표시줄 아이콘 중복 재생성 (버그)
* **문제**: `OnTimerTick`에서 `UpdateTitleForWindowState()`를 호출하는데, 이 메서드 내부에서 이미 `UpdateTaskbarHistory()`를 호출하고 있었습니다. 그런데 `OnTimerTick`이 바로 다음 줄에서 `UpdateTaskbarHistory()`를 또 호출하여, 최소화 상태에서는 `TaskbarHistoryIcon.Apply`(비트맵 생성 → GDI+ 드로잉 → `GetHicon()` → 아이콘 clone/destroy → `SendMessage(WM_SETICON)`)가 틱마다 두 배로 실행되고 있었습니다.

### 2. 타이틀바 강제 동기 리페인트
* **문제**: `InvalidateTitleBar()`가 `RDW_UPDATENOW` 플래그를 사용해, 창이 최소화 상태가 아니면 포커스/가시성과 무관하게 매 틱마다 `WM_NCPAINT`를 강제로 즉시(동기) 발생시키고 있었습니다.

### 3. 작업표시줄 스파크라인 아이콘의 높은 갱신 빈도
* **문제**: 최소화 상태에서 타이틀바 그래프를 표시하기 위한 아이콘(16px/32px)이 GPU 카운터 갱신(2초 간격)과 달리 매 틱(최소 200ms)마다 새로 생성되고 있었습니다. `GetHicon()`은 GDI 비트맵 핸들을 매번 새로 만드는 상대적으로 비용이 큰 호출입니다.

### 4. (경미, 미적용) 스파크라인 렌더링 시 브러시/펜 재할당
* `CpuSparklineRenderer.Draw`가 호출될 때마다 `SolidBrush`/`Pen`을 새로 할당합니다. 틱당 최대 5회(타이틀바 1 + 히스토리 패널 2 + 작업표시줄 아이콘 2사이즈) 발생하지만, .NET GC가 짧게 사는 소형 GDI+ 객체를 빠르게 처리하므로 체감 효과가 미미해 우선순위가 낮다고 판단, 이번 작업에서는 적용하지 않았습니다.

---

## 적용한 변경 내용

### 1. `MainForm.cs` — 작업표시줄 아이콘 중복 호출 제거
`OnTimerTick`의 마지막 `UpdateTaskbarHistory()` 호출을 제거했습니다. `UpdateTitleForWindowState()`가 이미 이를 처리하므로 동작 변화 없이 최소화 상태에서의 아이콘 재생성 비용이 절반으로 줄었습니다.

### 2. `MainForm.cs` / `NativeMethods.cs` — 타이틀바 리페인트 비동기화
`InvalidateTitleBar()`에서 `RDW_UPDATENOW` 플래그를 제거하여 `RDW_INVALIDATE | RDW_FRAME`만 사용하도록 변경했습니다. 리페인트가 일반 메시지 루프로 위임되어 매 틱마다 발생하던 강제 동기 블로킹이 사라졌습니다. 더 이상 쓰이지 않는 `RdwUpdatedNow` 상수도 `NativeMethods.cs`에서 정리했습니다.

### 3. `MainForm.cs` — 작업표시줄 아이콘 갱신 주기 완화
최소화 상태에서 `TaskbarHistoryIcon.Apply` 호출을 2초 간격으로 throttle 하도록 `_lastTaskbarIconUpdate` 필드와 `TaskbarIconUpdateInterval`(2초, GPU 카운터 갱신 주기와 동일)을 추가했습니다. 최소화 진입 시 첫 갱신은 즉시 반영되고, 이후부터는 2초 간격으로만 아이콘을 재생성합니다. 창을 복원하면 throttle 상태가 초기화되어 다음 최소화 시에도 즉시 반영됩니다.

---

## 검증 결과

### 자동 빌드 테스트
`dotnet build` 명령어로 컴파일 오류 없이 정상적으로 빌드 완료됨을 확인했습니다.
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### 동작 영향
* 일반(비최소화) 창 상태: 그래프(히스토리 패널)와 타이틀바 표시는 기존과 동일하게 매초 갱신됩니다.
* 최소화 상태: 작업표시줄 아이콘의 그래프 갱신이 최대 2초 지연될 수 있으나, 아이콘 재생성 및 관련 GDI 호출 빈도가 크게 줄어 CPU 사용량이 감소합니다.
