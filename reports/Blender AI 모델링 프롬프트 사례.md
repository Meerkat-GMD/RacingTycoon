# 숫자 명세와 검증 루프가 Blender AI 결과를 가른다

Blender와 AI를 함께 쓴 인기 결과물의 프롬프트는 의외로 짧았다. X에서 192만 회 조회된 blender-mcp 출시 데모는 "create a low poly dragon with a pot of gold" 한 문장으로 시작해 던전, 횃불, 날개 디테일을 덧붙이는 후속 지시 몇 번으로 끝났다. Simon Willison이 Codex로 만든 파스텔 톤 펠리컨도 "OK make it a whole lot better" 수준의 지시 세 번으로 완성됐다. 결과를 만든 것은 문장이 아니라 루프다. 에이전트가 장면 정보를 읽고 뷰포트를 캡처해 틀린 곳을 스스로 고쳤고, LL3M 같은 연구 시스템에서는 계획, 문서 검색, 5방향 렌더 비평을 맡은 에이전트들이 뒤에서 구조를 채웠다. 그런 파이프라인이 없는 1인 개발자는 이 구조를 프롬프트나 스킬 파일에 직접 적어야 한다. 들어갈 내용은 Blender 5.2 API 변경표, 미터와 머리 단위로 적은 비율, 4~12개 프리미티브 파트 목록, 스프라이트 카메라와 같은 정사영 검증 렌더, 삼각형 수와 떠 있는 파트를 코드로 확인하는 합격 기준이다. Meshy, Tripo, Rodin은 모두 "주제 먼저, 디테일 몇 개, 스타일은 마지막" 형식을 권하며, 폴리곤 수와 포즈와 토폴로지는 이제 프롬프트 단어가 아니라 설정값으로 조절한다. 다만 Meshy 커뮤니티 인기작은 88%가 이미지 입력이고 수십만~200만 면의 고밀도 메시다. 그래서 플랫 셰이딩 파스텔 토이 스프라이트에는 에이전트가 bpy로 프리미티브를 조립하는 방식을 기본으로 삼고, 생성기는 복잡한 소품과 참고 모델용 보조 수단으로 두는 것이 맞다.

## 조회수 192만 드래곤도 한 줄 프롬프트와 후속 지시로 만들어졌다

AI 에이전트가 Blender를 다룬 결과물 가운데 가장 널리 퍼진 것은 지금도 2025년 3월 Siddharth Ahuja가 올린 blender-mcp 출시 데모다. 이 게시물은 **X 조회 1,923,288회, 좋아요 11,429개, 북마크 12,018개**를 기록했다([X](https://api.fxtwitter.com/sidahuj/status/1899460492999184534)). 유튜브 설명에 따르면 첫 프롬프트는 "create a low poly dragon with a pot of gold"였고, 이어서 던전과 횃불과 더 정교한 날개를 추가하는 지시가 나왔다. 작성자는 Claude가 장면을 읽을 수 있기 때문에 출력이 틀리면 스스로 알아채고 고친다고 설명했다([YouTube](https://www.youtube.com/watch?v=DqgKuLYUv00)). 아래 표는 2026-09-27에 확인한 주요 사례의 프롬프트 공개 여부와 반응이다.

| 결과물 (날짜) | 도구와 모델 | 공개된 프롬프트 | 반응 |
|---|---|---|---|
| 로우폴리 드래곤과 금단지 (2025-03-11) | Claude Desktop + blender-mcp | "create a low poly dragon with a pot of gold", 이후 던전·횃불·날개 후속 지시 | [X 조회 192만, 북마크 1.2만](https://api.fxtwitter.com/sidahuj/status/1899460492999184534) |
| Poly Haven 해변 장면 (2025-03-17) | blender-mcp + Poly Haven | Poly Haven의 HDRI·텍스처·바위·식물로 "beach vibe"를 만들라는 한 문장 | [X 조회 15.5만](https://api.fxtwitter.com/sidahuj/status/1901632110395265452) |
| 모닥불 주변 오두막 마을 (2025-07-20) | Show HN MCP 서버 | 오두막 5채, 가운데 모닥불, 왼쪽 강, 나무다리를 배치하라는 한 문장 | [HN 151점, 댓글 61개](https://news.ycombinator.com/item?id=44622374) |
| 해변을 달리는 펠리컨 (2026-09-05) | GPT-6 Astra (Medium), Codex | 네 개 모두 공개 (아래 본문) | [HN 10점](https://news.ycombinator.com/item?id=49583457) |
| 건축 시각화 (2026) | GPT-6 Astra, Codex | 다섯 개 모두 공개 (아래 본문) | 반응 수치 미공개 |
| Opus 5.5 클레이메이션 (2026-09-22) | Claude Opus 5.5, claude.ai | 비공개, "단일 프롬프트"였다고만 언급 | [X 조회 25.7만](https://api.fxtwitter.com/alexalbert__/status/2102458348511879448) |
| 신칸센, 오브젝트 5,112개 (2026-09-22) | Opus 5.5 + Higgsfield | 비공개 (벤더 홍보) | [X 조회 13.1만](https://api.fxtwitter.com/higgsfield_ai/status/2102507018372436264) |
| 생물 리깅·애니메이션 (2026-09-23) | Opus 5.5 vs GPT-6 Astra | "Rig & Animation of 3D creature"라는 요지만 | [X 조회 4.1만](https://api.fxtwitter.com/Stefan_3D_AI/status/2102641562824135022) |

2026년의 확산은 모델 출시 시점에 몰렸고, 화제작일수록 프롬프트를 밝히지 않았다. Anthropic의 Alex Albert는 Opus 5.5가 claude.ai에서 프롬프트 하나로 Blender 클레이메이션을 만든다며 영상을 올렸지만 문장은 공개하지 않았다([X](https://api.fxtwitter.com/alexalbert__/status/2102458348511879448)). 좌석 430개까지 포함한 Higgsfield의 신칸센과 "게임 레디" 문어 역시 프롬프트 없는 벤더 홍보다([X](https://api.fxtwitter.com/higgsfield_ai/status/2102526940859232433)). 한 사용자는 같은 생물 리깅 작업에 Opus 5.5가 40분, GPT-6 Astra가 65분 걸렸다고 적었는데, 이 역시 한 사람의 일화다([X](https://api.fxtwitter.com/Stefan_3D_AI/status/2102641562824135022)). 7월 Opus 5 때 화제가 된 F1 쇼룸에는 **에이전트 약 50개, 토큰 약 1,000만 개, 10시간 이상**이 들었다는 2차 요약만 남아 있다([SaaSCity](https://saascity.io/blog/claude-opus-5-one-shot-3d-games-worlds-blender-2026)). 최근 화제작의 정밀도는 긴 실행 시간과 다중 에이전트 오케스트레이션에서 나왔으니, 한 번의 프롬프트로 재현할 수 있는 수준으로 보면 안 된다.

프롬프트 전문이 남은 사례를 보면 구조가 잘 드러난다. Simon Willison은 설치된 Blender로 자전거 타는 펠리컨을 렌더하라고 먼저 지시했다(2분 39초). 이어 "OK add a background and a lot of flair"(3분 51초)와 "OK make it a whole lot better"(5분 59초)를 차례로 줬고, 마지막으로 방금 배운 Blender 사용법을 스킬 문서로 정리하게 했다. 첫 결과부터 부드러운 파스텔 톤의 장난감 같은 모습이었다는 점은 질문자의 목표 스타일과 겹친다([Simon Willison TIL](https://til.simonwillison.net/llms/blender-coding-agents-macos)). OpenAI의 건축 시각화 사례는 더 의도적으로 짜여 있다. 먼저 분위기와 의도를 말하고, "Draw me a floor plan first"로 평면도부터 받아 검토했다. 큰 수정 전에는 현재 장면을 백업하게 했고, "Iterate until ... the geometry is correct"처럼 종료 조건을 문장에 넣었으며, 조명과 재질은 형태 검증과 따로 다뤘다. Astra는 미리보기 렌더를 보고 식재 겹침과 소파 위치를 스스로 고쳤다([OpenAI Developers](https://developers.openai.com/blog/architectural-visualization-with-astra)). 연구 쪽 결론도 같다. LL3M의 대표 결과는 "Create a mini cartoon character", "a red bucket" 같은 짧은 입력에서 나왔고 에이전트별 시스템 프롬프트도 한 문장씩이다. 그 대신 계획, 문서 검색, 코딩, 5방향 렌더 비평, 검증을 각기 다른 에이전트가 맡았다([LL3M](https://arxiv.org/html/2508.08228)). 짧은 프롬프트가 통한 것은 도구나 파이프라인이 구조를 대신 채웠기 때문이다.

## 에이전트에게는 형용사 대신 작업 순서와 합격 기준을 준다

### 도구마다 기본 루프가 다르니 먼저 알고 써야 한다

2026년 현재 Blender를 실시간으로 조작하는 방법은 크게 세 가지다. 커뮤니티판 MCP for Blender(ahujasid, 저장소 이름은 `mcp-for-blender`로 바뀜)는 **스타 29,397개**를 받았고, 장면 조회, 뷰포트 스크린샷, 코드 실행, Poly Haven·Sketchfab·Poly Pizza 검색, Rodin·Hunyuan3D·Tripo 생성을 포함한 36개 도구를 제공한다([GitHub API](https://api.github.com/repos/ahujasid/mcp-for-blender), [server.py](https://raw.githubusercontent.com/ahujasid/blender-mcp/main/src/blender_mcp/server.py)). Blender Foundation의 공식 Lab MCP 서버는 Blender 5.1 이상과 애드온 v1.0.3을 요구한다. 공식 예시 프롬프트는 폴리곤 수 분석이나 재질 사용처 찾기 같은 분석과 디버깅 위주이며, LLM이 만든 코드를 아무 보호 장치 없이 실행한다고 경고한다([blender.org](https://www.blender.org/lab/mcp-server/)). 세 번째는 MCP 없이 Codex가 `blender --background --python`으로 Blender를 직접 실행하는 방식으로, Simon Willison과 OpenAI 사례가 모두 이렇게 했다([OpenAI Developers](https://developers.openai.com/blog/architectural-visualization-with-astra)). 질문자처럼 스프라이트를 일괄 렌더하는 파이프라인에는 이 방식이 가장 단순하다.

커뮤니티 서버에는 작성자가 넣은 `asset_creation_strategy`라는 기본 프롬프트가 들어 있고, 사실상 이것이 모범 프롬프트다. 먼저 `get_scene_info()`를 호출하고, 변경 전후마다 `get_viewport_screenshot()`으로 결과를 확인하며, 가져온 오브젝트마다 world bounding box를 확인해 위치, 크기, 회전, 겹침을 고치라고 지시한다. 그런데 같은 전략은 스타일라이즈드 로우폴리 게임 에셋이 필요하면 Poly Pizza를, 독특한 단일 물체면 Hyper3D나 Hunyuan3D를 먼저 쓰게 한다. 스크립트 모델링은 연동이 모두 꺼졌거나 "A simple primitive is explicitly requested"일 때만 쓰는 대체 수단이다([server.py](https://raw.githubusercontent.com/ahujasid/blender-mcp/main/src/blender_mcp/server.py)). 따라서 통일된 파스텔 토이 스타일이 목표라면 사이드바에서 에셋 연동을 끄거나, 다운로드와 생성 없이 Python으로 프리미티브만 조립하라고 명시해야 한다. 그러지 않으면 다른 화풍의 외부 에셋이 섞인다. 같은 전략 문서는 AI 생성기를 단일 물체에만 쓰고 장면 전체나 바닥, 따로 만든 부품의 조립에는 쓰지 말라고 못 박는다.

### 블록아웃을 승인해 잠근 뒤 한 층씩 더한다

형태를 잡는 순서는 2026-09-24에 나온 Vagon의 참조 이미지 워크플로가 가장 구체적이다. 첫 요청에서는 에이전트에게 이미지를 분석하게 한다. 받을 내용은 주요 형태와 계층, 추정 카메라 위치와 초점 거리, 재질과 광원 방향, 가려지거나 모호한 부분, 스스로 세워야 할 가정의 목록이다. 다음으로 프리미티브 블록아웃을 만들어 참조와 비교하는데, 디테일보다 실루엣, 초점 거리, 상대 크기, 간격, 바닥 접지를 먼저 맞춘다. 이후 패스마다 승인된 카메라와 주요 치수와 컬렉션 구조는 고정하고 보조 형태나 재질 중 한 층만 더하게 한다. 한 라운드에는 가장 큰 불일치 세 개만 고치게 하고, 주 시점 외의 각도와 솔리드 셰이딩 진단 렌더도 함께 요구한다([Vagon](https://vagon.io/blog/reference-image-to-3d-scene-with-gpt-6-astra-blender)). 참조 이미지의 수도 결과를 바꾼다. Codex로 물체 9개를 재구성한 벤치마크를 보면 의자, 찻주전자, 도토리처럼 형태가 전형적인 물체는 렌더 한 장이 더 나았다. 반면 애니메 캐릭터, 기타, 닻, 주판, 숟가락은 정사영 6방향이 이겼고, 저자는 여러 시점을 하나로 합치는 과정이 가장 큰 병목이라고 결론 내렸다([codex-blender-bench](https://marknefedov.github.io/codex-blender-bench/)). 캐릭터처럼 전형적이지 않은 형태에는 정면, 측면, 윗면 턴어라운드를 주는 편이 낫다.

### 판단은 눈보다 숫자로 한다

형용사는 에이전트가 가장 약한 입력이다. 실무 가이드들은 "좀 더 자연스럽게" 같은 예술적 판단, Geometry Nodes, 복잡한 노드 그래프는 맡기지 말라고 한다. 대신 절차적 형상, 일괄 이름 변경과 재질 교체, 렌더 설정처럼 규칙이 분명한 일을 맡기라고 정리한다([CLSkills Hub](https://clskillshub.com/blog/claude-blender-3d-modeling-workflow)). 한 게임 개발 이슈는 캐릭터 요청을 형용사 대신 합격 기준으로 적었다. 기준은 캐릭터당 약 3천 삼각형 이하, 읽히는 실루엣, 한눈에 웃긴 모습이었다([GitHub issue #76](https://github.com/Cezart3/Escape-With-Your-Friends/issues/76)). OpenAI도 실행하고 검사하고 고치는 것까지가 작업이라면 그 과정을 요청에 명시하라고 권한다. 스킬 문서도 긴 절차 대신 판단 경계를 짧게 적으라고 한다([OpenAI Developers](https://developers.openai.com/blog/rethinking-skills-and-prompts-for-gpt-6-astra)). 검증은 스크린샷만으로는 부족하다. 3DCodeBench에서는 실행에 성공한 코드에도 떨어져 있거나 분리된 부품이 계속 남았다([3DCodeBench](https://arxiv.org/abs/2606.01057)). LL3M의 VLM 비평가는 떨어진 손잡이 같은 공간 오류를 놓쳤고([LL3M](https://arxiv.org/html/2508.08228)), 공식 커넥터로 일본 교실을 만든 실험에서는 의자가 책상을 관통했다([Zenn](https://zenn.dev/shintama/articles/blender-official-mcp-claude)). 가장 확실한 대응은 bounding box 겹침, 연결 요소 수, 삼각형 수를 코드로 출력하는 검사다. 비전 판단은 해석을 돕는 데만 쓴다([blender-ai-mcp](https://github.com/PatrykIti/blender-ai-mcp)).

한계도 분명하다. 2026년 8월의 한 분석은 Blender MCP를 뛰어난 Blender 조작자이지만 형편없는 모델러라고 요약했다. 캐릭터나 생물이나 식물은 **"늘린 구의 조립"**이 되고, 토폴로지에는 n-gon과 불균일한 밀도가 남으며, 이는 프롬프트로 고칠 수 없다는 것이다([3D AI Studio](https://www.3daistudio.com/3d-generator-ai-comparison-alternatives-guide/blender-mcp)). 256점을 받은 Hacker News 스레드에서도 스크립트 작성은 어느 지점까지는 훌륭하지만 그 뒤로는 전혀 나아지지 않는다는 반응과, 3D 공간 이해가 아직 부족하다는 반응이 나왔다([HN](https://news.ycombinator.com/item?id=47936370)). Claude Code로 로우폴리 장면을 만드는 유료 강좌도 수동 편집 단원을 따로 두고, 깔끔한 스타일라이즈드 룩에는 형태와 색과 구도에 대한 사람의 결정이 필요하다고 밝힌다([MammothClub](https://mammothclub.com/course/low-poly-stylization-with-blender-in-claude-code)).

## bpy 프롬프트는 "Blender 5.2.2"와 API 변경표로 시작한다

LLM이 쓴 bpy 코드가 실패하는 가장 흔한 이유는 모델링 실력이 아니라 API 버전이다. 2026년 5월에 나온 3DCodeBench(Blender 5.0, 212개 카테고리)는 실패가 주로 API 불일치에서 나온다고 보고했다. Gemini 2.5 Pro와 GPT-5.4 Nano는 4.x에서 5.0으로 바뀐 API 때문에 실패의 약 85%가 생겨 평가에서 빠졌다. 오류 트레이스백을 모델에 다시 넣어 주는 멀티턴 루프는 실행 성공률을 **0.702에서 0.974로** 끌어올렸다. 하지만 코딩 에이전트 하네스를 써도, 코드가 일단 돌아간 뒤의 형태 품질은 단일 프롬프트와 구별되지 않았다. 최상위급 모델은 사고 예산을 최소에서 최대로 올려도 실행률 차이가 5포인트 미만이었다([3DCodeBench](https://arxiv.org/html/2606.01057)). 실무에서 얻을 결론은 두 가지다. 트레이스백 루프는 반드시 돌리되, 형태 품질을 올리려면 자율 반복 횟수보다 숫자로 된 파트 명세와 작은 헬퍼 API에 공을 들여야 한다. LL3M에서도 Blender 문서 검색(RAG)을 붙이자 복잡한 연산 사용이 평균 1.20회에서 5.86회로 늘고 오류가 3.29회에서 2.43회로 줄었다. 에이전트끼리 코드 문맥을 공유하지 않으면 자동 수정 단계가 에셋을 처음부터 다시 만들어 원래 구조가 사라졌다([LL3M](https://arxiv.org/html/2508.08228)). 스크립트 하나를 유지하면서 바꿀 부분만 요청해야 결과가 안정된다.

모델의 학습 데이터는 Blender 2.8~4.x 코드가 대부분이므로, 5.2.2에서 깨지는 식별자를 프롬프트나 스킬 파일에 대체 표로 넣는 것이 가장 비용이 적은 개선책이다. 공식 릴리스 노트에서 확인한 변경은 다음과 같다.

| 버전 | 모델이 흔히 쓰는 옛 코드 | 5.2.2에서 쓸 코드 |
|---|---|---|
| 4.0 | Principled BSDF `Specular`, `Emission`, `Subsurface` 등 소켓 | `Specular IOR Level`, `Emission Color`, `Subsurface Weight` 등 ([4.0 노트](https://developer.blender.org/docs/release_notes/4.0/python_api/)) |
| 4.0 | `bpy.ops`에 context 딕셔너리 전달, `tree.inputs.new()` | `context.temp_override()`, `tree.interface.new_socket(...)` ([4.0 노트](https://developer.blender.org/docs/release_notes/4.0/python_api/)) |
| 4.1 | `use_auto_smooth`, `auto_smooth_angle`, `calc_normals_split` | 제거됨. `sharp_edge` 속성을 다루는 모디파이어나 연산자, `Mesh.corner_normals` ([4.1 노트](https://developer.blender.org/docs/release_notes/4.1/python_api/)) |
| 5.0 | `BLENDER_EEVEE_NEXT`, `scene.node_tree`, `action.fcurves`, BGL | `BLENDER_EEVEE`, `scene.compositing_node_group`, 레거시 Action API 제거, BGL 제거 ([5.0 노트](https://developer.blender.org/docs/release_notes/5.0/python_api/)) |
| 5.0 | Boolean 솔버 `FAST`, 렌더 패스 `Z` | `FLOAT`, `Depth` ([5.0 노트](https://developer.blender.org/docs/release_notes/5.0/python_api/)) |
| 5.1 | 이전 Python 버전을 전제한 코드 | Python 3.13 ([5.1 노트](https://developer.blender.org/docs/release_notes/5.1/python_api/)) |
| 5.2 | `modifier["Input_2"] = 5.0` | `modifier.properties.inputs.<identifier>.value = 5.0` ([5.2 노트](https://developer.blender.org/docs/release_notes/5.2/python_api/)) |

`Material.use_nodes`가 5.x에서 폐지됐는지, `shade_flat` 연산자의 동작이 바뀌었는지는 이번 조사에서 확인하지 못했다. 이 두 가지는 첫 스크립트를 실행해 직접 확인해야 한다.

플랫 셰이딩 게임 에셋용으로 공개된 템플릿 중 가장 완결된 것은 2026-08-28에 공개된 Claude Code 스킬 blender-lpm-skill이다. 스타는 4개뿐이고 캐릭터 몸체는 범위에서 제외하지만, 연구 결과가 권하는 구조를 그대로 담았다([SKILL.md](https://github.com/ozanzeng/blender-LPM-skill/blob/main/skills/blender-lpm-skill/SKILL.md)). 먼저 에셋, 실제 크기(m), 삼각형 예산, 팔레트, 대상 엔진을 한 줄 브리프로 적는다. 다음으로 물체를 4~12개 프리미티브 파트로 나누고 파트마다 팔레트 색과 대칭 여부를 적은 뒤, `box`, `prism`, `lathe`, `sweep` 같은 헬퍼 함수로 짧은 레시피 스크립트를 쓴다. 그다음 정사영 5방향 렌더 시트로 실루엣, 비율, 색 구분, 면 수를 판단해 고친다. 반복은 세 번이면 정상이고 다섯 번을 넘으면 파트 목록이 틀렸으니 처음으로 돌아가라고 한다. 마지막 게이트는 예산, 트랜스폼 적용, 바닥 z=0, 빈 머티리얼 슬롯과 퇴화 면과 고립 정점이 없는지를 확인한다. 스타일 규칙은 질문자의 룩에 그대로 쓸 만하다. 곡률은 서브디비전 대신 면 수로 표현해서 손잡이와 리벳은 6~8면, 돔과 축은 8~12면으로 만든다. 맞닿는 파트는 이웃에 2 mm 이상 파묻거나 2 mm 이상 띄워서 같은 평면이 겹쳐 깜빡이는 문제(z-fighting)를 막는다. 대칭은 미러로만 만들고, 넓은 면은 채도를 낮추고 포인트에만 채도를 주며, 각 색에 어두운 짝을 두어 아랫면에 쓴다([budgets-and-style.md](https://github.com/ozanzeng/blender-LPM-skill/blob/main/skills/blender-lpm-skill/references/budgets-and-style.md)). 공개 예시 브리프는 "Low-poly Roman loot chest, 1.2 m, ≤ 2 500 tris, wood + bronze, collider, Unity/URP" 한 줄이었고 결과물은 868 삼각형이었다([blender-LPM-skill](https://github.com/ozanzeng/blender-LPM-skill)).

이 근거들을 2.5등신 파스텔 토이 캐릭터에 옮기면 아래와 같은 프롬프트가 된다. 여러 출처를 조합한 추론이지 실제로 검증된 프롬프트는 아니며, 색상 값과 비율 수치는 예시다.

```text
[환경] Blender 5.2.2 (Python 3.13). `blender --background --python make_rabbit.py`로 실행되는
단일 스크립트를 쓴다. 실행할 때마다 장면을 비우고 같은 결과를 만든다.
[쓰지 말 것 → 대신 쓸 것] BLENDER_EEVEE_NEXT → BLENDER_EEVEE / use_auto_smooth 금지 /
scene.node_tree → scene.compositing_node_group / BSDF "Emission" → "Emission Color",
"Specular" → "Specular IOR Level" / modifier["Input_2"] → modifier.properties.inputs.<id>.value
[브리프] 토끼 캐릭터, 총 높이 1.0 m, 2.5등신(머리 지름 0.40 m), 삼각형 1,500개 이하, Unity 스프라이트용
[파트] 외부 에셋 다운로드와 AI 생성 금지. 프리미티브만 사용.
  head = UV sphere 12분할 / body = 8각 테이퍼 원기둥 / ears, arms, legs = 6각 prism, 좌우는 미러
  모든 치수는 머리 지름을 기준으로 계산 (예: 눈 높이 = 머리 높이의 0.45)
  맞닿는 파트는 이웃에 2 mm 이상 파묻고, 눈과 볼터치 원판은 머리 표면에서 2 mm 띄운다.
[팔레트] body #F4D8E4 / body_dark #E0B8C8 / eye #3B3441 / blush #F3A0B4, 파트당 단색 1개
[스타일] 모든 파트 Shade Flat. 서브디비전, 노멀맵, 아웃라인 없음.
[검증] 스프라이트 카메라(정사영, 3/4 시점)와 정면·측면·윗면을 PNG로 렌더한다.
  스크립트 끝에서 총 삼각형 수, 파트별 world bounding box, 최저점 z, 떨어진 파트 수를 출력한다.
  기준을 어기면 가장 큰 문제 3개만 고쳐 다시 실행한다. 5회를 넘기면 멈추고 파트 목록을 다시 제안한다.
[마무리] 색과 비율을 파일 상단 상수로 모아 재사용 가능한 스크립트로 정리하고,
  이번에 막힌 API와 해결법을 SKILL.md에 추가한다.
```

이 템플릿에서 가장 중요한 줄은 버전 표, 머리 크기에서 유도하는 비율, 파묻기 규칙, 숫자 출력 검증이다. 머리 지름을 기준으로 모든 파트를 계산하라는 지시는 LPM 스킬의 "비율을 비로 적는 감사표"와 치수 오차 ±5% 게이트를 캐릭터에 맞게 옮긴 것이다([qa-checklist.md](https://github.com/ozanzeng/blender-LPM-skill/blob/main/skills/blender-lpm-skill/references/qa-checklist.md)). LL3M에서 한 번의 지시로 성공한 사용자 수정은 약 59%였고, 인물 손에 아이스크림을 쥐여 주는 식의 공간 수정은 3~4번의 후속 지시가 필요했다([LL3M](https://arxiv.org/html/2508.08228)). 그러니 포즈나 소품 쥐기처럼 공간 관계가 복잡한 요청은 처음부터 좌표와 접촉점으로 적는 것이 낫다.

## 생성기 프롬프트는 주제-디테일-스타일 순서이고 폴리곤 수는 설정값이 정한다

Meshy, Tripo, Rodin, Hunyuan3D의 공식 가이드는 거의 같은 형식을 권한다. 주제를 맨 앞에 두고, 형태·재질·색에 관한 구체적 디테일 몇 개를 붙이고, 스타일 단어를 끝에 둔다. 품질 과장 단어("4k", "masterpiece")는 넣지 않는다. 게임용 제어는 이미 프롬프트 밖의 설정값으로 옮겨 갔다.

| 도구 | 권장 구조 | 길이와 네거티브 | 로우폴리·게임용 설정 | 포즈와 입력 |
|---|---|---|---|---|
| Meshy (Meshy 6, 7.1) | [주제] + [재질] + [스타일] + [기술 제약], 앞 단어일수록 영향이 큼 ([Meshy Docs](https://docs.meshy.ai/en/webapp/guides/prompting)) | 20~60단어 권장, API 800자, 네거티브 미지원 ([Meshy Help](https://help.meshy.ai/en/articles/11972484-meshy-prompt-guide-best-practices-and-templates-by-asset-type)) | Smart Topology `target_polycount` 100~15,000, quad/triangle, `remove_lighting` ([Meshy API](https://docs.meshy.ai/en/api/text-to-3d)) | `pose_mode` a-pose/t-pose |
| Tripo (H3.1, Smart Mesh P2.0) | 주제·형태 → 스타일·충실도 → 디테일 2~4개와 시점 → 제외·기술 조건 ([Tripo 블로그](https://www.tripo3d.ai/blog/explore/ai-3d-model-generator-prompt-templates-for-specific-asset-types)) | 1,024자(약 100단어), `negative_prompt` 255자 ([Tripo API](https://docs.tripo3d.ai/model-generation/text-to-model-v3-0-v3-1.html)) | `smart_low_poly` 1,000~20,000면, quad면 500~10,000, `texture=false`. P2.0은 삼각형 500~50,000, 쿼드 500~25,000, 파트 자동 분리 ([Tripo P2.0](https://www.tripo3d.ai/blog/tripo-p2-0-preview)) | P2.0은 정면·좌·우·후면 4방향 이미지 입력 |
| Rodin Gen-2.5 | 주제 먼저, 그다음 셰이딩·팔레트·모서리 처리·사용처 ([Hyper3D Low Poly](https://hyper3d.ai/styles/low-poly)) | 등급(tier) 명시 필수 ([Hyper3D Docs](https://docs.hyper3d.ai/en/api-specification/rodin-gen2-5)) | Raw/Quad/Smart Low-Poly, 목표 면 수 500부터 ([보도자료](https://www.barchart.com/story/news/2134263/hyper3d-launches-rodin-gen-2-5-bringing-sculpt-level-detail-and-production-control-to-ai-3d-generation)) | T/A 포즈 출력, 이미지 1~5장 |
| Hunyuan3D | {수량(1 권장)} + {주제}, {세부 묘사}, {스타일} ([Tencent Cloud](https://www.tencentcloud.com/document/product/1284/75290)) | 네거티브와 "4k" 불필요 | PolyGen 1.5 리토폴로지 ([Scenario](https://www.scenario.com/models/hunyuan-polygen-15)) | 해당 없음 |

어휘도 벤더마다 거의 같다. Rodin은 "low poly", "flat-shaded", "faceted", "one color per face"가 지오메트리와 텍스처를 함께 움직이고, 기존 로우폴리 에셋 이미지를 참조로 주면 방향이 더 확실히 고정된다고 설명한다. Rodin 공식 예시 가운데 "Low-poly fox, flat-shaded triangular facets, warm autumn palette, strong silhouette"가 이 형식을 가장 압축해 보여 준다([Hyper3D Low Poly](https://hyper3d.ai/styles/low-poly)). 같은 회사의 카툰 페이지는 둥근 실루엣, 과장된 비율, 밝은 무광 색을 세 가지 특징으로 꼽는다. "cartoon watering can, cherry red body, soft yellow spout, matte plastic look, white background"를 "장난감 플라스틱 레시피"라고 부르면서, 카툰 곡선은 둥글게 보일 만큼 폴리곤이 필요하고 일부러 면을 드러내면 그건 로우폴리라는 다른 스타일이라고 선을 긋는다([Hyper3D Cartoon](https://hyper3d.ai/styles/cartoon)). 흔한 실수도 있다. Tripo는 "highly detailed"와 "low poly"를 한 프롬프트에 넣지 말고 세부는 텍스처로 암시하라고 하며, AI가 정확한 대칭을 어려워한다고 인정한다([Tripo 블로그](https://www.tripo3d.ai/blog/explore/how-to-generate-low-poly-assets-with-ai)). Meshy는 네거티브 프롬프트 대신 "solid construction, single solid mesh"처럼 긍정형 문구로 떠 있는 부품을 막으라고 권한다([Meshy Help](https://help.meshy.ai/en/articles/11972484-meshy-prompt-guide-best-practices-and-templates-by-asset-type)). 가장 중요한 경고는 Meshy 튜토리얼의 한 문장이다. Smart Topology가 해결하는 것은 폴리곤 예산이지 룩이 아니며, 메시는 기본적으로 부드럽게 렌더된다([Meshy Tutorial](https://www.meshy.ai/tutorials/make-low-poly-3d-models)).

### Meshy 인기작의 88%는 이미지 입력이었다

프롬프트를 대량으로 공개하는 곳은 Meshy뿐이다. Meshy 커뮤니티의 공개 JSON에서 수집한 인기 게시물 1,773개 가운데 **1,566개(88%)가 이미지 입력**이었다. 좋아요 상위 100개 중 94개, 2026년 게시물의 93.7%도 이미지 기반이었고, 로우폴리(Smart Topology) 모드를 쓴 게시물은 **1,773개 중 5개**뿐이었다([Meshy community endpoint](https://www.meshy.ai/meshyd-api/web/public/community/posts?sortBy=likes&pageNum=1&pageSize=50)). 좋아요 1위인 "The Yelling Goblin"(1,154개)은 제목만 공개했다([Meshy](https://www.meshy.ai/posts/019fca8b-b1fe-7bdb-92c3-afed0c5a602a)). Tripo의 추천 갤러리에 오른 18개 항목은 프롬프트 칸이 모두 비어 있었고([Tripo Explore](https://studio.tripo3d.ai/explore)), Rodin 커뮤니티 페이지는 404를 반환했다([Hyper3D](https://hyper3d.ai/3d-prompts)). 이미지 입력 게시물에 붙은 긴 설명은 아마 레퍼런스 이미지를 만들 때 쓴 프롬프트겠지만, Meshy는 이를 그렇게 표시하지 않는다. 질문자의 스타일과 가까운 귀여운 캐릭터, 토이, 점토, 로우폴리, 탈것 게시물은 다음과 같다(2026-09-27 기준).

| 게시물 (날짜) | 좋아요 / 다운로드 | 입력과 모델 | 면 수 | 프롬프트 구성과 핵심 문구 |
|---|---|---|---|---|
| [Pip](https://www.meshy.ai/posts/019ff5f8-d491-73a3-8d1f-d7c42bd049dc) (2026-08-12) | 671 / 74 | 이미지, Meshy 7 | 1,946,728 | "tiny adorable chibi" 동반자 → 둥근 몸·이끼·큰 눈 → 배 속 불빛 → "Kawaii 3D render style" → 흰 배경·스튜디오 조명·전신 |
| [잉어 드래곤](https://www.meshy.ai/posts/019feedb-3e5b-72eb-b522-2e59d4897e71) (2026-08-11) | 131 / 10 | 텍스트, Meshy 6+7 | 387,476 | 작은 동반자 → 비단잉어 무늬 → "compact silhouette" → "no water effects, no text, no floating fins" |
| [구름 양](https://www.meshy.ai/posts/019feed0-070f-7ff5-86aa-872e8bdf7cd2) (2026-08-11) | 70 / 6 | 텍스트, Meshy 6 | 미확인 | "compact toy-like proportions, clean silhouette" → "no floating pieces" |
| [달 토끼](https://www.meshy.ai/posts/019feecc-ec2c-7f01-b317-e10c4d3842d2) (2026-08-11) | 64 / 11 | 텍스트, Meshy 6 | 미확인 | "dreamy collectible toy aesthetic" → "neutral standing pose" → "no text, no base, no floating elements" |
| [Pop Mart 호박 임프](https://www.meshy.ai/posts/019a0576-18e1-761a-ac1c-fbc2dfb2a3fe) (2025-10-21) | 85 / 8 | 이미지, meshy-5.3 | 미확인 | "Pop Mart style chibi" → 큰 호박 가면·짧은 팔 → "Glossy painted vinyl finish" |
| [점토 치비 해골](https://www.meshy.ai/posts/0199dad2-6703-7854-a81c-384257defa23) (2025-10-12) | 57 / 58 | 이미지, meshy-5.3 | 미확인 | "Matte clay surface" → 큰 머리와 손 → 상아색 그라데이션 |
| [로우폴리 트롤](https://www.meshy.ai/posts/019f813a-ee43-7464-93b3-964a6067e11b) (2026-07-20) | 75 / 36 | 이미지, Meshy 6 | 미확인 | 외형 묘사 뒤에 "low-poly stylized game art, clean topology, humanoid A-pose, full body" |
| [RC 버기](https://www.meshy.ai/posts/01a0183b-7dc0-7c9d-9d90-a3dc3bee0587) (2026-08-19) | 121 / 21 | 이미지, Meshy 7 | 미확인 | Body / Finish / Detail 세 단락으로 나눈 긴 설명, 네온 그린 셸·크롬 휠 |

인기 있는 귀여운 캐릭터 프롬프트에는 공통 골격이 있다. "tiny … companion" 같은 크기와 귀여움의 훅으로 시작하고, "round, chubby, compact, oversized head" 같은 체형 단어를 붙인다. 이어 대표 디테일 두세 개, "matte clay"나 "glossy vinyl" 같은 재질 비유, "full body, clean silhouette, neutral standing pose, white background" 같은 구도가 나오고, 마지막에 "no text, no base, no floating pieces" 같은 제외 문구가 본문에 들어간다. 이 구성은 위의 벤더 가이드와 정확히 일치한다. 하지만 이 게시물들을 질문자 파이프라인의 템플릿으로 삼으면 안 된다. 확인한 두 게시물은 38만~195만 면의 텍스처 입힌 PBR 메시였고, "vinyl, clay, plush" 같은 재질 단어는 매끈하고 둥근 형태를 만든다. 인기 프롬프트에서 가져올 것은 컨셉 이미지 단계에서 쓸 어휘이지, 로우폴리 단색 메시가 곧바로 나온다는 증거가 아니다. RC 버기 프롬프트는 부위별로 단락을 나누는 방식이 레이싱 게임 소품의 컨셉 이미지를 만들 때 참고할 만하다.

### 캐릭터는 컨셉 이미지를 먼저 만들고 텍스처 없이 생성한다

스타일라이즈드 캐릭터라면 이미지부터 만드는 것이 2026년의 사실상 표준이다. Meshy는 흰 배경이나 투명 배경, 정면이나 약간 비스듬한 각도, T 포즈나 중립 서 있는 포즈, 그림자가 구워지지 않는 고른 확산광, 1024 px 이상, 화면의 70~90%를 채우는 피사체를 권한다. Midjourney나 DALL-E로 만든 이미지라면 "white background, studio lighting, front view"를 붙이라고 하고, 정면·측면·후면 여러 장을 주면 보이지 않는 면을 지어내는 일이 줄어든다고 한다([Meshy Help](https://help.meshy.ai/en/articles/15723519-how-to-get-better-image-to-3d-results-in-meshy)). Tripo는 이미지 프롬프트 끝에 "Front-facing, centered, isolated on white background, even lighting, no shadows."를 붙이는 템플릿을 제시한다. 극적인 카메라 각도, 모션 블러, 강한 반사는 피하라고 하며, Smart Mesh의 기본 결과는 약 5,000면이다([Tripo 블로그](https://www.tripo3d.ai/blog/nano-banana-image-to-3d-workflow)). 이를 조합하면 질문자용 컨셉 이미지 프롬프트는 "[subject], cute toy figure, about 2.5 heads tall, big round head, tiny body, dot eyes, pink blush circles, pastel [colour] palette, matte plastic, low poly flat-shaded facets, full body, front view, A-pose, arms slightly away from body, centered, plain white background, even flat lighting, no shadows" 형태가 된다. 소품이라면 Rodin의 장난감 레시피를 따라 "low poly toy race car, chunky proportions, oversized wheels, pastel mint and cream two-tone palette, flat-shaded facets, one color per face, single solid mesh, no decals, white background"처럼 쓸 수 있다. 두 프롬프트 모두 출처의 어휘를 조합한 추론이고 실제로 시험해 보지는 않았다. 머리와 몸의 비율이 생성 과정에서 유지되는지를 다룬 공식 자료도 찾지 못했다.

생성한 뒤의 정리는 Blender에서 한다. 점 눈과 볼터치는 이 크기에서 텍스처로 구워지거나 혹처럼 튀어나오기 쉽다. 그래서 Tripo의 `texture=false`처럼 텍스처 없이 지오메트리만 받고, 눈과 볼터치는 Blender에서 작은 메시나 머티리얼 슬롯으로 붙이는 편이 깔끔하다(추론). Tripo P2.0은 몸, 옷, 액세서리를 별도 조각으로 나눠 주므로 파트별 단색 머티리얼을 입히기 쉽다([Tripo P2.0](https://www.tripo3d.ai/blog/tripo-p2-0-preview)). 면 수가 많으면 Decimate의 Planar 모드로 평평한 면을 합치거나 Collapse 비율로 줄인다([Blender Manual](https://docs.blender.org/manual/en/latest/modeling/modifiers/generate/decimate.html)). 텍스처 색을 유지하려면 Diffuse 베이크에서 Color만 켠다. 그러면 조명과 무관한 표면 색만 얻고, 출력 대상을 Active Color Attribute로 두면 정점 색으로 바로 굽힌다([Blender 5.2 Manual](https://docs.blender.org/manual/en/latest/render/cycles/baking.html)). 굽힌 색을 고정 팔레트로 맞추는 기능은 Blender에 기본으로 없으므로 짧은 Python 단계가 필요하다. 마지막으로 Shade Flat을 적용한다. 생성기의 텍스처에 조명이 이미 구워졌다면 Meshy의 `remove_lighting`을 켜거나, 애초에 평평한 조명으로 컨셉 이미지를 만드는 것이 유일한 예방책이다([Tripo](https://www.tripo3d.ai/game-development/remove-baked-lighting-shadows-image-to-3d-ai-game-assets)).

## 파스텔 토이 캐릭터는 bpy로 조립하고 생성기는 소품과 참고용으로 쓴다

질문자의 스타일에는 에이전트의 약점이 오히려 장점이 된다. 에이전트가 만든 유기체가 "늘린 구의 조립"이 된다는 비판은([3D AI Studio](https://www.3daistudio.com/3d-generator-ai-comparison-alternatives-guide/blender-mcp)), 둥근 프리미티브를 이어 붙이는 파스텔 토이 캐릭터에서는 그대로 스타일의 문법이다. 최종 산출물이 스프라이트라는 점도 유리하다. 비판에서 지적된 쿼드 토폴로지, UV 레이아웃, PBR 베이크가 결과물에 거의 영향을 주지 않고, 단색 머티리얼은 UV 약점을 아예 피해 간다. 남는 약점은 비율과 매력, 파트 사이의 접합, 캐릭터 세트 전체의 일관성이다. 이것들은 앞의 템플릿처럼 턴어라운드 참조, 숫자 비율, 고정 팔레트, 재사용 스크립트로 다룰 수 있다(추론). 반대로 Poly Pizza, Sketchfab, AI 생성기에서 가져온 에셋은 출처마다 화풍과 스케일과 원점이 다르다. 커뮤니티 MCP의 기본 전략이 가져온 에셋마다 크기 정규화와 bounding box 확인을 강제하는 것도 그 때문이다([server.py](https://raw.githubusercontent.com/ahujasid/blender-mcp/main/src/blender_mcp/server.py)). 생성기는 레이싱 게임의 복잡한 차량처럼 프리미티브로 만들기 번거로운 소품이나 형태 참고용으로 쓰고, 가져온 뒤에는 팔레트 머티리얼로 교체하고 Shade Flat을 적용해 스타일을 맞춘다.

재현성 측면에서도 스크립트 방식이 낫다. LL3M README는 같은 프롬프트에서도 결과가 달라질 수 있다고 경고한다([LL3M GitHub](https://github.com/threedle/ll3m)). LPM 스킬은 레시피 스크립트를 에셋과 함께 보관하게 하고([SKILL.md](https://github.com/ozanzeng/blender-LPM-skill/blob/main/skills/blender-lpm-skill/SKILL.md)), Simon Willison은 세 번의 반복 끝에 에이전트가 스스로 스킬 문서를 쓰게 했다([Simon Willison TIL](https://til.simonwillison.net/llms/blender-coding-agents-macos)). LLM을 모델을 직접 만드는 도구가 아니라, 늘 같은 결과를 내는 생성 스크립트를 작성하는 작성자로 쓰는 편이 맞다. 그래야 몇 달 뒤 색 하나를 바꿀 때도 스크립트 상수만 고쳐 전체 스프라이트를 다시 렌더할 수 있다.

생성기를 상업 게임에 쓴다면 라이선스를 먼저 확인해야 한다. 한국에서 개발하는 경우 특히 그렇다.

| 서비스 | 무료 플랜 | 유료 플랜 | 비고 |
|---|---|---|---|
| Meshy | 상업 사용 가능, CC BY 4.0 표기 필요 ([Meshy Help](https://help.meshy.ai/en/articles/9992001-can-i-use-my-generated-assets-for-commercial-projects)) | 생성물 소유 | Blender 플러그인이 5.2.0까지 테스트됨 ([Meshy Docs](https://docs.meshy.ai/en/webapp/plugins/blender/introduction)) |
| Tripo | 상업 권리 없음, Tripo가 권리 보유 ([Tripo Help](https://www.tripo3d.ai/help/privacy-policy/how-to-use-tripo-models-commercially)) | 사용·수정·배포·상업화 전권 | v3.0/v3.1 모델 내보내기는 유료 전용 |
| Rodin (Hyper3D) | 비공개 에셋 10개, 권리 명시 없음 | Creator 월 30달러부터 "Unlimited export and any use" ([Hyper3D Pricing](https://hyper3d.ai/pricing)) | Blender 애드온 있음 |
| Hunyuan3D 오픈 웨이트 | 라이선스가 **한국, EU, 영국에는 적용되지 않음** ([LICENSE](https://github.com/Tencent-Hunyuan/Hunyuan3D-2.1/blob/main/LICENSE)) | 해당 없음 | Tencent Cloud 호스팅 API 약관은 미확인 |
| TRELLIS.2 | MIT ([GitHub](https://github.com/microsoft/TRELLIS.2)) | 해당 없음 | 이미지 입력만, VRAM 24 GB 이상 |
| SPAR3D | 연매출 100만 달러 미만 무료 ([Stability AI](https://stability.ai/news-updates/stable-point-aware-3d)) | 그 이상은 엔터프라이즈 라이선스 | 단일 이미지 입력 |

커뮤니티 MCP의 Hunyuan3D 경로를 쓰기 전에는 Tencent Cloud 호스팅 서비스의 약관을 따로 확인해야 한다. 보안도 챙겨야 한다. 커뮤니티 MCP 작성자의 GitHub 계정이 2026-08-09에 탈취됐다가 복구됐으므로(해당 공지 조회 1,024,641회), 설치는 검증된 저장소와 패키지 이름으로 해야 한다([X](https://api.fxtwitter.com/sidahuj/status/2086445625147793503)). `BLENDER_MCP_SAFE_MODE=1`을 켜면 파일 입출력, 서브프로세스, 네트워크 호출을 막으면서 모델링, 렌더, 저장, 가져오기와 내보내기는 그대로 쓸 수 있다. 텔레메트리는 기본적으로 최소한의 익명 사용 기록만 수집한다. 다만 도구 설명이 모델에게 사용자 원문을 매 호출마다 `user_prompt`로 넘기라고 지시하고, 수집에 동의하면 프롬프트와 코드와 스크린샷이 AI 학습에 쓰일 수 있으므로 `DISABLE_TELEMETRY=true`를 검토할 만하다([README](https://raw.githubusercontent.com/ahujasid/mcp-for-blender/main/README.md)). 공식 Lab MCP는 생성 코드를 보호 장치 없이 실행하므로, 코드를 실행하기 전에는 항상 파일을 저장해 두는 습관이 필요하다([blender.org](https://www.blender.org/lab/mcp-server/)).

## 결론

"어떤 프롬프트를 쓰는가"에 대한 답은 문장을 다듬는 데보다 산출물의 형태를 바꾸는 데 있다. 인기 사례의 한 줄 프롬프트가 통한 것은 모델이 똑똑해서라기보다, 장면을 읽고 렌더를 보고 고치는 루프를 도구가 대신 돌려 줬기 때문이다. 연구 결과는 그 루프가 코드를 실행되게 만들 뿐 형태를 좋게 만들지는 못한다는 점까지 보여 준다. 형태 품질은 숫자로 된 파트 명세와 헬퍼 API에서 나온다. 1인 개발자에게 실제로 쌓이는 자산은 좋은 프롬프트 모음이 아니다. 팔레트 상수, 파트 빌더 함수, API 변경표, 스프라이트 카메라 설정을 담은 스킬 파일과 캐릭터별 생성 스크립트다. 이것이 갖춰지면 다음 캐릭터의 프롬프트는 "토끼를 곰으로, 긴 귀를 둥근 귀로"처럼 달라진 점만 적으면 된다.

불확실한 부분도 분명하다. 에이전트가 스타일라이즈드 캐릭터를 만든 과정의 전체 프롬프트 로그는 공개된 것이 없고, 어떤 프롬프트 요소가 품질을 올리는지 같은 과제로 비교한 실험도 찾지 못했다. Opus 5.5와 GPT-6 Astra의 리깅·애니메이션 시연은 프롬프트도 재현 로그도 없는 홍보 게시물이다. Reddit 반응은 이번 조사에서 접근하지 못했다. 그러니 처음 만드는 캐릭터 3~5개에서 반복 횟수, 실패 유형, 삼각형 수를 직접 기록해 자기 파이프라인의 기준선을 세우는 편이, 외부 화제작을 따라 하는 것보다 더 정확한 근거가 된다.
