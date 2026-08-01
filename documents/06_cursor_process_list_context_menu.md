# Process list context menu
_Exported on 5/24/2026 at 20:37:41 GMT+9 from Cursor (3.5.33)_

---

**User**

프로세스 목록을 출력했을 때, 프로세스 목록에서 오른쪽 버튼을 누르면 "Kill Process"라는 메뉴 항목을 만들어 줘. 이 항목을 선택하면 해당 Process를 kill하는 거야.

---

**Cursor**

프로세스 목록 UI와 컨텍스트 메뉴 구현 위치를 먼저 찾겠습니다.


MainForm.cs를 확인해 UI 언어와 구조를 맞추겠습니다.


코드베이스 확인 완료. WinForms `ListView`에 컨텍스트 메뉴를 추가하는 계획을 작성합니다.


**Kill Process 동작 확인**

> **프로세스 종료 전 확인 대화상자를 표시할까요?**
> - 예 — 프로세스 이름/PID 확인 후 종료
> - 아니오 — 메뉴 선택 즉시 종료 ✔

> **자식 프로세스도 함께 종료할까요? (예: 브라우저 탭/워커 프로세스)**
> - 예 — 프로세스 트리 전체 종료 (Process.Kill(entireProcessTree: true)) ✔
> - 아니오 — 선택한 PID만 종료

---

**User**

Implement the plan as specified, it is attached for your reference. Do NOT edit the plan file itself.

To-do's from the plan have already been created. Do not create them again. Mark them as in_progress as you work, starting with the first one. Don't stop until you have completed all the to-dos.

---

**Cursor**

계획대로 `MainForm.cs`에 Kill Process 컨텍스트 메뉴를 구현합니다.


구현을 마쳤고 빌드도 성공했습니다. [`MainForm.cs`](d:\Work\TempWork\_ProgrammingProject\WindowsCpuBar\WindowsCpuBar\MainForm.cs)에 다음을 추가했습니다.

1. **컨텍스트 메뉴** — 프로세스 목록 우클릭 시 **Kill Process** 항목 표시
2. **PID 저장** — `UpdateProcessList()`에서 각 행의 `Tag`에 PID 저장
3. **종료 처리** — 선택 시 확인 없이 `Process.Kill(entireProcessTree: true)` 호출, 성공하면 목록에서 해당 행 제거
4. **보호 로직** — 자기 프로세스(`Environment.ProcessId`) 종료 시도는 차단하고 메시지 표시
5. **메뉴 활성화** — 선택된 행이 없으면 Kill Process 항목 비활성화

**목록 새로고침**으로 프로세스 목록을 연 뒤, 행을 우클릭해 동작을 확인하면 됩니다.

---

**User**

Context Menu항목에 "Process Info"항목도 추가해 줘. 그러면 Popup Windows에 실행파일의 실제 위치 등 정보를 표시해 줘. Popup Windows에서는 Kill버튼, Open Location버튼, Close버튼을 아래쪽에 위치시켜줘. Kill버튼은 해당 process를 kill하고, Open Location은 파일 탐색기로 실행파일의 위치를 여는거야. Close버튼은 창을 닫으면 돼.

---

**Cursor**

Process Info 팝업 추가를 위해 현재 컨텍스트 메뉴와 프로세스 관련 코드를 확인합니다.


코드베이스와 기존 Kill Process 구현을 확인했습니다. Process Info 팝업 추가 계획을 작성합니다.


---

**User**

Implement the plan as specified, it is attached for your reference. Do NOT edit the plan file itself.

To-do's from the plan have already been created. Do not create them again. Mark them as in_progress as you work, starting with the first one. Don't stop until you have completed all the to-dos.

---

**Cursor**

Process Info 팝업 구현을 시작합니다. 먼저 `ProcessKillHelper.cs`부터 추가합니다.


SubItems 인덱스를 수정합니다 — CPU는 `[1]`, 메모리는 `[2]`입니다.


구현을 완료했고 빌드도 성공했습니다.

### 추가된 파일

**[`ProcessKillHelper.cs`](d:\Work\TempWork\_ProgrammingProject\WindowsCpuBar\WindowsCpuBar\ProcessKillHelper.cs)**  
- `TryKill(pid)` — 자기 프로세스 보호 + `Process.Kill(entireProcessTree: true)` 공통 처리

**[`ProcessInfoForm.cs`](d:\Work\TempWork\_ProgrammingProject\WindowsCpuBar\WindowsCpuBar\ProcessInfoForm.cs)**  
- 프로세스 이름, PID, CPU, 메모리, 실행 파일 경로, 시작 시간 표시
- 하단 버튼: **Kill** / **Open Location** / **Close**
- Kill 성공 시 `ProcessWasKilled = true` 후 창 닫기
- Open Location은 `explorer.exe /select,"경로"` 실행 (경로 접근 불가 시 버튼 비활성화)

### 수정된 파일

**[`MainForm.cs`](d:\Work\TempWork\_ProgrammingProject\WindowsCpuBar\WindowsCpuBar\MainForm.cs)**  
- 컨텍스트 메뉴에 **Process Info** 추가 (Kill Process 위)
- Process Info 선택 시 팝업 표시, Kill로 종료되면 목록에서 해당 행 제거
- Kill Process도 `ProcessKillHelper` 사용
- Opening 핸들러가 모든 메뉴 항목 활성/비활성 처리

**목록 새로고침** → 프로세스 우클릭 → **Process Info**로 동작을 확인하면 됩니다.

---

**User**

프로세스 정보를 보여주는 창에서 실해파일 위치 아래에 현재 작업폴더(current working directory)를 보여 줘.

---

**Cursor**

Process Info 창에 작업 폴더 필드를 추가하기 위해 현재 구현과 Windows에서 다른 프로세스의 CWD를 읽는 방법을 확인합니다.


Windows에서 다른 프로세스의 작업 폴더는 PEB를 읽어야 하므로, 헬퍼를 추가한 뒤 Process Info 창에 필드를 넣겠습니다.


Process Info 창에 **작업 폴더** 필드를 추가했습니다.

### 변경 사항

**[`ProcessWorkingDirectoryHelper.cs`](d:\Work\TempWork\_ProgrammingProject\WindowsCpuBar\WindowsCpuBar\ProcessWorkingDirectoryHelper.cs)** (신규)  
- Windows PEB에서 다른 프로세스의 current working directory를 읽는 헬퍼
- 64비트 / WOW64(32비트) 프로세스 모두 지원
- 권한이 없거나 읽을 수 없으면 `null` 반환

**[`ProcessInfoForm.cs`](d:\Work\TempWork\_ProgrammingProject\WindowsCpuBar\WindowsCpuBar\ProcessInfoForm.cs)**  
- **실행 파일** 아래에 **작업 폴더** 행 추가
- 접근 불가 시 기존과 같이 `(접근할 수 없음)` 표시
- 창 높이를 약간 늘려 긴 경로도 표시 가능하게 조정

Process Info를 열면 실행 파일 경로 바로 아래에 해당 프로세스의 현재 작업 폴더가 표시됩니다.

