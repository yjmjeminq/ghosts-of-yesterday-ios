# Spider-Gwen · Ghosts of Yesterday

첨부 설정집을 바탕으로 제작한 한국어 오프라인 비주얼 노벨입니다. 1~50화와 베란다 에필로그를 한 개의 선형 이야기로 연결했습니다.

## Windows 빌드

`Builds/Windows/GhostsOfYesterday.exe` 옆에 있는 Unity 데이터 폴더와 라이브러리를 함께 보관합니다. 프로젝트 루트의 `게임 실행.cmd`는 Windows 빌드의 바로가기입니다.

- Space 또는 Enter로 읽습니다. H는 대사창 숨김, Esc는 메뉴입니다.
- 타이틀의 **이어 읽기**는 마지막 자동 저장을 불러옵니다. **불러오기**에는 수동 저장 6칸과 빠른 불러오기가 있습니다.
- 이야기 화면에서 **메뉴 → 저장과 불러오기**로 수동 저장을 사용합니다. F5는 빠른 저장, F9는 빠른 불러오기입니다.
- **CG 갤러리**에서 본 장면의 CG나 캐릭터가 포함된 합성 장면을 다시 봅니다. **챕터**에서 이미 도달한 회차를 본편 저장에 영향을 주지 않고 회상합니다.

## 제작 및 배치 검사

Unity 6000.3.27f1을 사용합니다. `Assets/Game/Scenes/Novel.unity`가 시작 장면입니다. `story/episodes`를 수정한 뒤 `python tools/compile_story.py`로 런타임 JSON을 갱신합니다. 장면 헤더의 선택적인 마지막 필드에 `gwen_standing@930,647,540`처럼 스프라이트 ID와 화면 속 앵커 X·Y, 높이를 적으면 배경에 인물을 합성합니다. 여러 등장인물은 세미콜론으로 구분합니다.

`tools/build_windows.ps1`은 50화 순서와 대본·에셋, 저장 슬롯 및 이전 버전 저장 호환성을 검사한 다음 에디터 배치 빌드를 수행합니다. `python tools/preview_layers.py`는 Unity나 게임을 열지 않고 베란다 합성 확인 이미지를 출력합니다.

현재 결과와 남은 검증은 `docs/PROJECT_STATE.md`, CG 출처는 `docs/ASSET_MANIFEST.json`을 참조합니다.
