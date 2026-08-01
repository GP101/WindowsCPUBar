# Windows CPU performance monitoring application
_Exported on 5/19/2026 at 10:29:52 GMT+9 from Cursor (3.4.20)_

---

**User**

Windows 11에서 현재 CPU의 Performance를 Start button에 배치된 태스크바에 progress bar정보로 표시해 주는 Windows 응용 프로그램을 만들어 줘. 일반 창 모드에서는 일반 윈도우 창에서 progress bar의 update주기, 색, 텍스트 표시 여부를 설정할 수 있어야 해. 프로그램이 최소화되면 CPU의 performance를 title bar에 보여주는 거야.

---

**Cursor**

Windows 11 태스크바에 CPU 성능을 progress bar로 표시하는 앱을 만들겠습니다. 먼저 워크스페이스 상태를 확인합니다.


빈 프로젝트입니다. WinForms와 Windows Taskbar API(ITaskbarList3)로 구현합니다.


오버레이 텍스트 표시를 개선하고 빌드 경고를 확인합니다.


**Windows CPU Bar** WinForms 앱을 추가했습니다. .NET 8 기준이며, Release 빌드는 성공했습니다.

## 실행 방법

```powershell
dotnet run --project "d:\Work\TempWork\_ProgrammingProject\WindowsCpuBar\WindowsCpuBar\WindowsCpuBar.csproj"
```

또는 실행 파일:

`d:\Work\TempWork\_ProgrammingProject\WindowsCpuBar\WindowsCpuBar\bin\Release\net8.0-windows\WindowsCpuBar.exe`

(.NET 8 Desktop Runtime 필요)

## 동작

| 기능 | 설명 |
|------|------|
| **작업 표시줄 진행 표시** | `ITaskbarList3`로 앱 버튼(시작 버튼 옆)에 CPU 사용률(0–100%) 표시 |
| **일반 창** | 갱신 주기(200–10000 ms), 진행 표시줄 색상, 텍스트 표시 on/off 설정 |
| **최소화** | 제목 표시줄에 `CPU 45%` 형식으로 표시 |
| **설정 저장** | `%LocalAppData%\WindowsCpuBar\settings.json` |

## 프로젝트 구조

- `CpuMonitor.cs` — `GetSystemTimes`로 CPU 사용률 계산
- `TaskbarProgress.cs` — 작업 표시줄 progress API
- `ProgressBarHelper.cs` — 창 안 progress bar 색상
- `MainForm.cs` — 설정 UI 및 타이머 갱신
- `AppSettings.cs` — 설정 로드/저장

## 참고

- **작업 표시줄** progress bar 색상은 Windows가 정하며(보통 앱 강조 색), API로 바꿀 수 없습니다. **색상 설정**은 창 안 progress bar에만 적용됩니다.
- 첫 샘플 직후부터 정확한 CPU 값이 나옵니다(두 번째 샘플부터 델타 계산).

실행해 보시고, 트레이 최소화·시작 프로그램 등록 같은 기능이 필요하면 알려 주세요.

