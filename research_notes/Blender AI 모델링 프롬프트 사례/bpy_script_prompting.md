# Prompting LLMs to write Blender Python (bpy) scripts that build 3D models (state as of 2026-09-27)

Scope note: this file covers research systems, benchmarks and practitioner templates for LLM-written bpy modeling code. MCP live-control tools and text-to-3D services are deliberately not covered. Each source carries its date; findings from 2023-2024 papers used GPT-4/GPT-4V-era models and may understate what 2026 models (GPT-5.5, Claude Opus 4.7, Gemini 3.x) can do.

## 1. What research systems and benchmarks report about prompt design and agent loops

### Takeaway
Every successful research system splits the job into (a) a structured plan or part decomposition written before code, (b) code written against a small, well-documented API (retrieved docs, a helper library, or procedural functions) rather than raw vertices, and (c) a loop that feeds execution errors and rendered images back to the model. The newest benchmark, 3DCodeBench (May 2026, Blender 5.0), finds that feeding back tracebacks almost removes crashes (executability rises from 0.702 to 0.974), but extra agent turns do not improve shape quality once the code runs. Floating or disconnected parts remain the main geometric failure, and 2025-2026 systems such as Procedura answer it with explicit, machine-checked part attachments ("mates").

### Cited Findings

**3DCodeBench: Benchmarking Agentic Procedural 3D Modeling via Code (submitted 2026-05-31; Blender 5.0)**
- The task: from text or image input, a policy writes a Blender Python script that is executed into a mesh. The benchmark scores binary executability and mesh-grounded similarity in single-turn (T=1) and multi-turn (T>1) settings. It covers 212 Infinigen categories: flora, fauna, furniture, kitchenware and architectural fragments. Reference code has a median of 387 lines and a mean of 531, and creatures, trees and cabinets exceed 1,000 lines. — [arXiv HTML 2606.01057](https://arxiv.org/html/2606.01057)
- Scores are executability / SigLIP-2 / Elo. GPT-5.5: 0.906 / 0.834 / 1,163. Claude Opus 4.7: 0.910 / 0.814 / 1,006. Gemini 3.1 Pro: 0.725 / 0.824 / 1,147. GPT-5.4: 0.866 / 0.817 / 1,074. Claude Sonnet 4.6: 0.804 / 0.813 / 1,015. Gemini 3.5 Flash: 0.464 / 0.824 / 1,119. GPT-5.4 mini: 0.731 / 0.803 / 951. Claude Haiku 4.5: 0.502 / 0.761 / 799. — [arXiv HTML 2606.01057](https://arxiv.org/html/2606.01057)
- Multi-turn error feedback with truncated Blender tracebacks raised aggregate executability from 0.702 (single turn) to 0.974 (+27.2 points), and SigLIP-2 improved in all 22 model×track cells (aggregate +0.128). — [arXiv HTML 2606.01057](https://arxiv.org/html/2606.01057)
- "Coding-agent harnesses lift executability further but conditional shape quality remains indistinguishable from single prompt." Harnesses fix "simple API usage errors" but do not produce semantically richer geometry once the code compiles. — [arXiv HTML 2606.01057](https://arxiv.org/html/2606.01057)
- On thinking budget: "Lightweight reasoners are considerably more sensitive to increased thinking budgets than heavier models: Gemini 3.1 Flash Lite gains approximately 19 executability points from minimal to high, whereas Pro-class backbones change by fewer than five points." Frontier models plateau early; Claude Opus already plateaus at minimal thinking. — [arXiv HTML 2606.01057](https://arxiv.org/html/2606.01057)
- Failure taxonomy, quoted: "Failures mostly arise from API mismatches, while successful renders still suffer from disconnected or floating 3D geometric components." — [arXiv abs 2606.01057](https://arxiv.org/abs/2606.01057)
- Gemini 2.5 Pro and GPT-5.4 Nano were excluded because Blender 4.x→5.0 API drift caused about 85% of their failures. — [arXiv HTML 2606.01057](https://arxiv.org/html/2606.01057)
- The system prompts are in Appendix C: text-to-3D (C.1), image-to-3D (C.2), multi-turn error-feedback templates (C.3) and visual self-critique prompts (C.4). They were not quoted in the retrieved text. Project page: www.3dcodebench.com, which also hosts the 3DCodeArena ranking. — [arXiv HTML 2606.01057](https://arxiv.org/html/2606.01057), [arXiv abs](https://arxiv.org/abs/2606.01057)

**LL3M: Large Language 3D Modelers (arXiv 2508.08228v1, 2025-08-11; Blender 4.4)**
- LL3M uses six agents orchestrated with AutoGen: Planner (GPT-4o), Retrieval (GPT-4o, querying a "BlenderRAG" knowledge base), Coding (Claude 3.7 Sonnet), Critic (Gemini 2.0 Flash), Verification (Gemini 2.0 Flash) and a User Proxy. All agents share code context through an orchestrator. — [arXiv HTML 2508.08228](https://arxiv.org/html/2508.08228)
- The pipeline has three stages:
  - Initial creation: the planner splits the task into subtasks T1..Tn, retrieval returns docs, and the coder writes code with error-correction loops.
  - Auto-refinement: the critic renders **5 views** from adaptive camera distances and sends them to a VLM with a predefined critique prompt. The verifier re-renders to confirm that fixes landed.
  - User-guided refinement: the user sends text edits and the verifier checks them.
  — [arXiv HTML 2508.08228](https://arxiv.org/html/2508.08228)
- BlenderRAG was built from the Blender 4.4 docs, converted to 1,729 PDF files in RAGFlow. With RAG, the count of complex operations rose from 1.20 to 5.86 on average (about 5×) and the error rate fell from 3.29 to 2.43 (about 26%). — [arXiv HTML 2508.08228](https://arxiv.org/html/2508.08228)
- Agent ablation: quality improves step by step from coder only, to +planner, to +retrieval, to +critic. Without shared code context, auto-refinement "regenerates assets from scratch" and the outputs change substantially; with shared context, refinements keep the original structure. — [arXiv HTML 2508.08228](https://arxiv.org/html/2508.08228)
- Reported failures:
  - VLM critics can miss spatial artifacts such as disconnected handles.
  - About 59% of user edits succeed with one instruction; complex spatial edits, such as putting ice cream in a character's hands, need 3-4 follow-ups.
  - Auto-refinement does not always fix all flaws.
  — [arXiv HTML 2508.08228](https://arxiv.org/html/2508.08228)
- Repo `threedle/ll3m`: 553 stars (GitHub API, 2026-09-27), created 2025-08-06, last push 2026-03-07. The README states "The model used in the paper, Claude Sonnet 3.7, has been retired. As a result, we have discontinued the LL3M server." The repo is only a thin client (main.py, blender/addon.py, config), so the agent prompts are not published in code. — [GitHub threedle/ll3m](https://github.com/threedle/ll3m)

**Procedura: Agentic 3D Modeling with Procedural Control (arXiv 2608.26238, 2026-08-26)**
- Procedura represents an object as "a parametric program whose named parts are joined by typed, machine-checkable mates". The agent writes parts one at a time, "solving each placement from the mated frames rather than guessing it, and admitting a part only once compile, mate, and connectivity checks pass". A decoupled vision critic then refines the result. — [arXiv 2608.26238](https://arxiv.org/abs/2608.26238)
- The authors report that it beats native 3D generators on P3D-Bench and MechBench-36, has "the sharpest edges of any method", and surpasses "every prior 3D-code agent" on judged quality. The abstract does not name the engine, and no numbers were retrieved. Project page: https://spatiaos.github.io/projects/procedura/ — [arXiv 2608.26238](https://arxiv.org/abs/2608.26238)

**SceneCraft (arXiv 2403.01248, 2024-03-02, ICML 2024; GPT-4 / GPT-4V era, likely outdated as a model baseline)**
- The pipeline works in two loops:
  - Inner loop: decompose the query into sub-scenes, then build a relational scene graph (proximity, alignment, parallelism). The LLM-Coder turns the graph into Blender Python with numeric constraint/scoring functions, and GPT-4V reviews renders to revise the script.
  - Outer loop: "library learning" aggregates improved constraint functions into reusable skills without fine-tuning.
  — [arXiv HTML 2403.01248v1](https://arxiv.org/html/2403.01248v1)
- The extracted results table reports CLIP similarity 24.7 (BlenderGPT) vs 69.8 (SceneCraft) and constraint score 5.6 vs 88.9. The ablations score 48.3 without the learned library, 32.8 without the inner loop and 19.4 without the relational graph. Caveat: the metric scaling looks unusual for CLIP scores, so verify against the paper's table. Per the extracted summary, the full prompts are in Appendix E ("Prompt Used at each stage"). — [arXiv HTML 2403.01248v1](https://arxiv.org/html/2403.01248v1)
- The search summary says SceneCraft addresses objects "floating in mid-air" by introducing constraints between objects. — [search result summary of SceneCraft](https://arxiv.org/html/2403.01248v1)

**BlenderGym (arXiv 2504.01786, CVPR 2025 Highlight)**
- 245 hand-crafted start/goal scene pairs over 5 tasks: procedural geometry, lighting, procedural material, blend shape and object placement. Humans outperform all VLMs by about 2-10× on most metrics; for example, blend-shape photometric loss is 0.934 for humans vs 9.140 for GPT-4o. — [blendergym.github.io](https://blendergym.github.io/)
- The study used a generator-verifier pipeline. "VLM verifiers used for guiding generation also benefit from inference scaling," and there is a "Goldilocks ratio" of compute between generation and verification; ratios of 0.33, 0.62 and 0.73 verification share were tested. Models were GPT-4o, Claude 3.5 Sonnet, Gemini 1.5 Flash, Qwen2-VL and others (2025 generation, now outdated). — [blendergym.github.io](https://blendergym.github.io/), [arXiv 2504.01786](https://arxiv.org/abs/2504.01786)

**BlenderAlchemy (arXiv 2404.17672, ECCV 2024)**
- BlenderAlchemy treats editing as iterative search over a base .blend plus a modular Python script. A VLM proposes "Tweak" edits (numeric changes) and "Leap" edits (structural node-graph changes) and judges the renders. The motivating point: without seeing renders, LLMs operate "blindly". — [HF paper page](https://huggingface.co/papers/2404.17672), [GitHub BlenderAlchemyOfficial](https://github.com/ianhuang0630/BlenderAlchemyOfficial)

**VIGA: Vision-as-Inverse-Graphics Agent (arXiv 2601.11109, 2026-01-16, revised 2026-04-06)**
- VIGA runs a "tightly coupled code-render-inspect loop: synthesizing symbolic programs, projecting them into visual states, and inspecting discrepancies to guide iterative edits". It keeps skills and an "evolving multimodal memory" and is training-free. Over one-shot baselines it reports +35.32% on BlenderGym and +124.70% on its new BlenderBench. — [arXiv 2601.11109](https://arxiv.org/abs/2601.11109)

**3D-GPT (arXiv 2310.12945, 2023-10-19, revised 2024-05-29; oldest, GPT-3.5/4 era)**
- 3D-GPT uses three agents: task dispatch, conceptualization (enriches the description) and modeling (extracts parameter values that drive procedural generation in Blender). The retrieved abstract did not detail the Infinigen function docs in the prompts. — [arXiv 2310.12945](https://arxiv.org/abs/2310.12945)

**MeshCoder (arXiv 2508.14879, NeurIPS 2025)**
- MeshCoder turns point clouds into editable Blender Python by first designing "a comprehensive set of expressive Blender Python APIs". These APIs sweep a 2D section curve along a trajectory, bridge different sections, add bevels or booleans on basic shapes, and repeat shapes in 1D or 2D. Code is decomposed into semantic parts. The dataset has 1M object-code pairs over 41 categories, and the released artifacts include a checkpoint and 100K training pairs. — [arXiv 2508.14879](https://arxiv.org/abs/2508.14879), [GitHub InternRobotics/MeshCoder](https://github.com/InternRobotics/MeshCoder)

**ShapeCraft (arXiv 2510.17603, 2025-10-20, NeurIPS 2025 poster)**
- ShapeCraft introduces "a Graph-based Procedural Shape (GPS) representation that decomposes complex natural language into a structured graph of sub-tasks". Agents parse the input hierarchically into the GPS and then iteratively refine the procedural modeling and texturing. The paper claims better geometric accuracy than prior LLM agents; numbers were not retrieved. — [arXiv 2510.17603](https://arxiv.org/abs/2510.17603)

**Proc3D (arXiv 2601.12234, 2026-01)**
- Proc3D uses a "Procedural Compact Graph (PCG)" representation, which the search summary says reduces context overhead "by 4-10X compared to platform-specific codes like Blender". — [arXiv 2601.12234 (search summary)](https://arxiv.org/pdf/2601.12234)

**BlenderLLM (FreedomIntelligence)**
- BlenderLLM is a fine-tuned LLM "specifically designed to generate CAD scripts based on user instructions" that are executed in Blender. Its numbers and stars were not retrieved. — [GitHub FreedomIntelligence/BlenderLLM](https://github.com/FreedomIntelligence/BlenderLLM)

### Inferences
- The evidence points the same way across papers. **The plan and part structure decides shape quality; the error loop decides whether the code runs.** 3DCodeBench shows that harnesses and extra turns mostly fix crashes, not form. SceneCraft's largest ablation drop comes from removing the relational graph. LL3M improves most when a planner and critic are added. For a solo developer, effort is better spent on the spec (a numeric part list) and a small helper API than on longer autonomous agent loops.
- A small domain API beats raw bpy. MeshCoder (custom sweep/bridge/bevel APIs), 3D-GPT (procedural functions), Proc3D (compact graph, 4-10× less context) and the practitioner LPM skill (`lpm.py`, see section 2) all give the model a vocabulary of high-level part builders instead of letting it emit raw vertex lists.
- Floating and disconnected parts are the dominant failure that survives into 2026 (3DCodeBench), and VLM critics are unreliable at catching them (LL3M). So add **programmatic checks** (bounding-box contact or overlap tests, connected-component counts) rather than relying only on a model looking at renders. Procedura does exactly this with its connectivity and mate checks.
- With frontier models (Opus 4.7, GPT-5.5), a larger thinking budget buys little for this task (3DCodeBench). Iteration with real tracebacks and renders matters more.

### Gaps
- The verbatim appendix prompts of 3DCodeBench (Appendix C.1-C.4), SceneCraft (Appendix E) and LL3M's critique prompt were not retrieved. The LL3M repo does not contain them.
- EZBlender (arXiv 2601.07143, Plan-and-ReAct agent), ProcFunc (arXiv 2604.26943) and ArtLLM (arXiv 2603.01142) appeared in searches but were not read.
- No numeric Procedura or ShapeCraft comparisons against LL3M were retrieved.
- None of the benchmarks target stylized or chibi characters specifically. 3DCodeBench's categories come from Infinigen (organic and man-made objects).

## 2. Practitioner prompt templates and system prompts for reliable bpy code (incl. Blender 4.x→5.2 API drift)

### Takeaway
Practitioners and forum threads agree on one point above all: stating the Blender version up front matters most, because deprecated API calls are the single most common failure. 3DCodeBench independently confirms that API mismatch is the top failure class. The most complete published template for flat-shaded game assets is the Claude Code skill `blender-lpm-skill` (2026-08). It fixes the brief format (asset, size in metres, triangle budget, palette, engine), a 4-12-part primitive decomposition, a helper library, a five-view orthographic render review with an iteration cap, and automated gates. For Blender 5.x, the API changes most likely to break model-written code are:

- EEVEE engine renamed (`BLENDER_EEVEE_NEXT` → `BLENDER_EEVEE`)
- compositor `scene.node_tree` removed
- Principled BSDF socket renames (from 4.0)
- auto-smooth removal (from 4.1)
- legacy Action f-curve API removed
- Geometry Nodes modifier input access changed in 5.2

### Cited Findings

**blender-lpm-skill (ozanzeng, created 2026-08-28, 4 stars; MIT; very new, not yet widely validated)**
- Self-description: "Blender Low Poly Modelling Skill — build stylized, flat-shaded, game-ready low-poly assets ... from primitives with a fixed triangle budget, a single palette material, Unity-ready FBX + BaseColor/MaskMap export, and a measured render-inspect loop." — [SKILL.md](https://github.com/ozanzeng/blender-LPM-skill/blob/main/skills/blender-lpm-skill/SKILL.md), [repo](https://github.com/ozanzeng/blender-LPM-skill)
- Verbatim procedure (excerpt): — [SKILL.md](https://github.com/ozanzeng/blender-LPM-skill/blob/main/skills/blender-lpm-skill/SKILL.md)
  > 1. **Brief → budget.** Write one line: *asset, real size in metres, triangle budget, palette cells, target engine.* Budgets: hero weapon 1 200 · shield/helmet 1 500 · prop 800 · kit module 600–3 000 · character 8 000
  > 2. **Part list.** Decompose the object into 4–12 primitive parts, top-down (a gladius = blade, ridge, guard, grip, rings, pommel, rivet). Give each part a palette colour. Note which parts are mirrored.
  > 3. **Recipe script.** Write a short Python recipe using `lpm` (copy the pattern from `examples/`): `box`, `prism`, `lathe`, `sweep`, `plate`, then `bend` / `taper` / `mirror_x` / `rotate` / `move`. Metres, Z up, front = −Y. Finish with `lpm.finish(name, parts, palette, budget)` and `lpm.export_unity(...)`.
  > 4. **Look at it.** Render five views + contact sheet ... `--ortho` Read the sheet. Judge silhouette (front, side), proportion (compare against the brief's metres), read-ability of parts (do colours separate parts?), facet count (is any curve wasting triangles?). Fix the recipe, re-run. Three iterations is normal; more than five means the part list was wrong — redo step 2.
  > 5. **Reference overlay** when a concept exists: ... → IoU ≥ 0.90, aspect diff ≤ 3 %, then argue about details.
  > 6. **Gates.** ... PASS required: budget, transforms applied, base at z = 0, UVs present, no empty slots, no degenerate faces, no loose vertices.
- Verbatim style rules (excerpt): — [SKILL.md](https://github.com/ozanzeng/blender-LPM-skill/blob/main/skills/blender-lpm-skill/SKILL.md)
  > - **Flat shading everywhere**; curvature comes from facet count: 6–8 sides for grips and rivets, 8–12 for domes and shafts, 10–16 for hero wheels and shields. Never subdivide.
  > - **Silhouette > surface.** Spend triangles where the outline changes (tips, guards, crests, capitals); flat regions are single quads.
  > - **PBR = definitions, not textures.** One material, one 8×N-cell palette image (`Closest` interpolation), each face's UV in the centre of its cell. Every cell is a *definition*: `(name, baseColor, metallic, roughness[, emission])` ...
  > - **Real scale.** 1 unit = 1 m ... Base on z = 0, centred on x = 0, pivot at the base centre (props) or the snapping corner (kit modules).
  > - **Symmetry by mirror**, never by remodeling; asymmetric details only when the reference has them.
  > - **No coplanar faces between parts**: sink a part ≥ 2 mm into its neighbour or let it stand ≥ 2 mm proud. Coplanar twins z-fight and render black in Cycles. `finish()` welds only inside each part for the same reason.
- Verbatim "Do not" list: — [SKILL.md](https://github.com/ozanzeng/blender-LPM-skill/blob/main/skills/blender-lpm-skill/SKILL.md)
  > - Do not decimate a high-poly mesh and call it low-poly; rebuild from primitives. Do not call generators.
  > - Do not add a normal map to fake detail on a flat-shaded asset.
  > - Do not accept an asset from one flattering view; the side and top views expose thickness and proportion errors.
  > - Do not exceed the budget "just a little": remove a facet ring, merge two parts, or drop a rivet.
- Example brief: *"Low-poly Roman loot chest, 1.2 m, ≤ 2 500 tris, wood + bronze, collider, Unity/URP"*. It produced `SM_RomanChest` with 868 triangles. Recipe excerpt: `head = lpm.sweep("head", [...], depth=0.02, at=(0, 0, 1.15), color=P["iron"])` … `axe = lpm.finish("SM_Axe", [head, haft], P, budget=900)`. — [repo README](https://github.com/ozanzeng/blender-LPM-skill)
- The QA checklist includes "Dimensions match the brief (±5 %); base on z = 0; centred on x = 0", "Five-view sheet reviewed: front / side / back / three-quarter / top", "Parts readable by colour at distance; no two similar greys touching" and "Flat shading only; no smooth faces; no normal map". — [qa-checklist.md](https://github.com/ozanzeng/blender-LPM-skill/blob/main/skills/blender-lpm-skill/references/qa-checklist.md)
- Palette guidance: "Pair each hue with a darker shade (wood / wood_dark) for recesses and undersides — cheap fake AO" and "Keep saturation moderate for large areas ..., saturated only on accents". Facet table: rivet 6, grip 8, dome 8–12, column 10–16. The skill notes: "Character (body) ... this skill covers equipment; bodies use character-artist + retopology". — [budgets-and-style.md](https://github.com/ozanzeng/blender-LPM-skill/blob/main/skills/blender-lpm-skill/references/budgets-and-style.md)
- Reference-image mode: "**turnaround first** ... then the **audit table** (parts → primitives/kit builders, proportions as ratios, camera angle, palette, organic parts), then recipe ... max 3 iterations, accept by IoU/aspect + the 5-view sheet." — [SKILL.md](https://github.com/ozanzeng/blender-LPM-skill/blob/main/skills/blender-lpm-skill/SKILL.md)

**Forum and practitioner consensus on API versions**
- A practitioner guide on ChatGPT for Blender says outdated API names are the usual cause of errors and one round trip typically fixes them. It adds that "deprecated API calls from older Blender versions are the single most common failure" and recommends stating your version up front and asking for "a bpy script that…" rather than an explanation. This comes from a search summary and was not independently fetched. — [blenderai.org/models/chatgpt](https://blenderai.org/models/chatgpt)
- BlenderNation (2024-12 and 2025-01) covered paid add-ons that auto-test generated scripts and send failures back to GPT-4o with Blender API and Manual lookups. The underlying complaint: "Blender Scripts from ChatGPT Often Fail If You're Not a Coder". — [BlenderNation 2024-12-11](https://www.blendernation.com/2024/12/11/tired-of-code-errors-from-your-chatgpt-blender-add-on-heres-the-fix/), [BlenderNation 2025-01-14](https://www.blendernation.com/2025/01/14/blender-scripts-from-chatgpt-often-fail-if-youre-not-a-coder-this-blender-add-on-changes-that/), [Blender Artists thread](https://blenderartists.org/t/blender-scripts-from-chatgpt-often-fail-if-youre-not-a-coder-this-blender-add-on-changes-that/1573554)
- Open-source add-ons that embed a system prompt plus RAG over the Blender docs include `prompt2Blend` (system-prompt customization plus document retrieval) and `rougem/blender-ai-assistant` (Blender 4.5, AST static analysis sandbox before execution). — [prompt2Blend](https://github.com/Technologic101/prompt2Blend), [blender-ai-assistant](https://github.com/rougem/blender-ai-assistant)

**API changes that break model-written code (relevant to Blender 5.2.2)**
- **4.0 (Principled BSDF sockets):** "`Subsurface` -> `Subsurface Weight`, `Specular` -> `Specular IOR Level`, `Transmission` -> `Transmission Weight`, `Coat` -> `Coat Weight`, `Sheen` -> `Sheen Weight`, `Emission` -> `Emission Color`". — [4.0 Python API notes](https://developer.blender.org/docs/release_notes/4.0/python_api/)
- **4.0 (other):**
  - `bpy.ops` context override arguments were removed; use `context.temp_override()`.
  - Node group `tree.inputs.new()` / `tree.outputs.new()` were replaced by `tree.interface.new_socket(name=..., in_out='INPUT')`.
  - `MeshEdge.bevel_weight` and `MeshEdge.crease` became attributes (`bevel_weight_edge`, `crease_edge`, …).
  — [4.0 Python API notes](https://developer.blender.org/docs/release_notes/4.0/python_api/)
- **4.1 (auto smooth):**
  - "`use_auto_smooth` is removed. Face corner normals are now used automatically if there are mixed smooth vs. not smooth tags."
  - "`auto_smooth_angle` is removed. Replaced by a modifier (or operator) controlling the `"sharp_edge"` attribute."
  - `calc_normals_split` and related functions were removed in favour of `Mesh.corner_normals`.
  — [4.1 Python API notes](https://developer.blender.org/docs/release_notes/4.1/python_api/)
- **5.0:**
  - `BLENDER_EEVEE_NEXT` → `BLENDER_EEVEE`.
  - `scene.use_nodes` and `world.use_nodes` are deprecated; per the notes, "Currently it always returns `True` and setting it has no effect".
  - `scene.node_tree` was removed; use `scene.compositing_node_group`.
  - BGL was removed entirely.
  - The boolean solver "FAST" became "FLOAT".
  - The legacy Action API (`action.fcurves`, `action.groups`, `action.id_root`) was removed.
  - Dict-style access such as `bpy.context.scene['cycles']` for `bpy.props` properties was removed.
  - Render passes were renamed ("Z" → "Depth" and others).
  — [5.0 Python API notes](https://developer.blender.org/docs/release_notes/5.0/python_api/)
- **5.1:** Python was upgraded to 3.13. The notes list no direct changes to shading, camera or orthographic rendering. — [5.1 Python API notes](https://developer.blender.org/docs/release_notes/5.1/python_api/)
- **5.2 LTS:** Geometry Nodes modifier inputs moved from `modifier["identifier"] = 5.0` to `modifier.properties.inputs.identifier.value = 5.0`, and socket identifiers of the Compare and Random Value nodes changed. `gpu.init()` was added for background mode. — [5.2 Python API notes](https://developer.blender.org/docs/release_notes/5.2/python_api/)

### Inferences
- For a Blender 5.2.2 generator, the system prompt should name the exact version and list the renamed or removed identifiers above as a "do not use" or "use instead" table. Every model's training data is dominated by 2.8-4.x code (`use_auto_smooth`, `BLENDER_EEVEE_NEXT`, old BSDF socket names, `action.fcurves`, `modifier["Input_2"]`). The 3DCodeBench finding that 4.x→5.0 drift caused about 85% of two models' failures shows this is not hypothetical.
- The LPM skill's structure matches the research findings:
  - one-line numeric brief → a plan/spec (as in LL3M's planner)
  - part list → decomposition (as in ShapeCraft and SceneCraft)
  - `lpm.py` → domain API (as in MeshCoder and 3D-GPT)
  - five-view ortho sheet → visual critic (LL3M uses 5 views)
  - gates → programmatic checks (as in Procedura)

  It is the closest published template to the user's pipeline, but it has 4 stars and explicitly excludes character bodies.
- "Deterministic/idempotent script that clears the scene", "fixed seed" and "collection naming" were not found as quoted rules in any fetched source. They are common sense for the user's generator but lack a citation here.

### Gaps
- No fetched source shows a published, widely shared system prompt for general bpy generation, such as one on Blender StackExchange with high votes. Most "ChatGPT + Blender" guides found were vendor or SEO pages (3daistudio, clskillshub, harknessai) and were not fetched or trusted.
- The Blender 5.0 notes as retrieved did not say whether `Material.use_nodes` is deprecated. Check the full notes before telling the model to omit it.
- Whether `bpy.ops.object.shade_flat()` or `shade_auto_smooth` behaviour changed in 5.x was not covered by the retrieved notes.

## 3. Stylized low-poly / toy / chibi characters: prompt details that help, and common failure modes

### Takeaway
No peer-reviewed benchmark targets chibi or toy characters written in bpy. The best evidence is indirect: LL3M's "Create a mini cartoon character" showcase, the LPM skill's primitive-and-numbers discipline, and the benchmark failure lists. Together they suggest that a prompt should give:

- proportions as numbers (in metres or head units)
- a part list built from primitives with explicit attachment points
- mirror symmetry
- a rule for overlap or proud offsets between touching parts
- facet counts for curvature under flat shading
- palette values

The failures to guard against are floating or disconnected parts, coplanar z-fighting at joints, wrong scale, spatial edits that need several rounds, and API drift.

### Cited Findings
- LL3M's showcased character prompt is "Create a mini cartoon character", followed by the iterative edits "Add a blonde wig on the head" and "Make the character sit down and eat the ice cream with both hands". The spatial edit needed 3-4 follow-ups. — [arXiv HTML 2508.08228](https://arxiv.org/html/2508.08228)
- The LL3M README gives an organic example prompt: "Create a 3D object of a cartoon blue fish with streamlined body, large expressive eyes, flowing tail fins". — [GitHub threedle/ll3m](https://github.com/threedle/ll3m)
- The LPM skill has proportion and facet rules, not character rules:
  - "curvature comes from facet count ... 8–12 for domes"
  - "Symmetry by mirror"
  - "sink a part ≥ 2 mm into its neighbour or let it stand ≥ 2 mm proud"
  - "Do not accept an asset from one flattering view"

  The reference mode asks for an "audit table (parts → primitives/kit builders, proportions as ratios, camera angle, palette, organic parts)" and offers organic stand-ins (`blob`, `wedge`, `limb`), described as "faceted, readable, cheap". — [SKILL.md](https://github.com/ozanzeng/blender-LPM-skill/blob/main/skills/blender-lpm-skill/SKILL.md)
- Disconnected or floating components persist even in code that runs. — [3DCodeBench](https://arxiv.org/abs/2606.01057)
- VLM critics miss disconnected handles. — [LL3M](https://arxiv.org/html/2508.08228)
- Procedura's remedy is to solve placement from mated frames and admit a part only after connectivity checks pass. — [Procedura](https://arxiv.org/abs/2608.26238)
- Mirror-modifier practice in low-poly character tutorials enables clipping to avoid gaps at the centre seam (search summary; the specific tutorial was not fetched). — [search result: Tripo blog low-poly character guide](https://www.tripo3d.ai/blog/collect/creating-a-low-poly-character-in-blender--a-step-by-step-guide-m2udfkvkll4)

### Inferences
These apply the cited principles to the user's 2.5-head pastel toy; they are not directly sourced.

- State the height in head units and metres, and derive every part from the head size. Example: total 1.0 m tall; head diameter 0.40 m; body and legs 0.60 m; eyes at 0.45 of head height, 0.12 head apart; blush discs below and outside the eyes. This mirrors the LPM "proportions as ratios" audit table and the ±5% dimension gate.
- Build each part from one primitive with an explicit facet count. For example: head as a UV sphere or icosphere at about 8–12 segments per the dome rule, body as a tapered prism or lathe, limbs as low-sided prisms. Keep flat shading; do not subdivide.
- Place dot eyes and blush as separate small geometry standing ≥ 2 mm proud of the head surface (the LPM coplanar rule), or as palette-coloured faces. Never place them exactly on the surface, which causes z-fighting.
- Specify each joint as "part B is sunk X mm into part A at anchor point P". Then have the script assert contact, for example with a bounding-box overlap or a connected-component count after joining. This addresses the floating-part failure that renders and VLMs miss.
- Match the review renders to the shipping camera: orthographic front, side and 3/4 at the sprite angle. The LPM skill already uses `--ortho` five-view sheets, and LL3M uses 5 views.
- Put the palette as exact values in the prompt, as named hex cells with a darker twin for undersides (LPM palette guidance). For pastel work, keep large areas at moderate saturation and make only accents such as blush saturated.

### Gaps
- No source quantified which character-prompt details (head ratio, primitive list) improve LLM output. There is no ablation on stylized characters.
- No fetched practitioner post quoted a full chibi or toy-character bpy prompt verbatim with its result.
- Orthographic sprite-rendering prompt conventions (camera `ortho_scale`, Workbench vs EEVEE flat colour) were not covered by any fetched source.

## 4. Most-cited or shared example results and their exact prompts

### Takeaway
The most visible LL3M results came from short, plain prompts, not long specs: "a red bucket", "Create a mini cartoon character", or a one-sentence description with materials. The quality came from the agent pipeline (planner, RAG, 5-view critic), not from prompt wording. Its published agent system prompts are one sentence each. Star counts show LL3M (553) as the most-starred code-agent repo checked; the LPM skill is new (4 stars) but publishes the most complete template.

### Cited Findings
- LL3M agent system prompts, verbatim from Appendix A.2 — [arXiv HTML 2508.08228](https://arxiv.org/html/2508.08228):
  - Planner: "You are a planning agent. Your job is to break down complex tasks into smaller, manageable subtasks."
  - Retrieval: "You are a helpful assistant who can retrieve information from the knowledge base."
  - Coding: "You are a helpful assistant who can write Blender bpy code. You will be given a task, and you will need to write the code to complete the task."
  - Critic: "You are a helpful assistant who can critique the scene. You will use the critique_scene_tool to critique the scene."
  - Verification: "You are a helpful assistant who can verify the scene. You must use the verify_scene_tool to verify the scene."
- LL3M showcase prompts, verbatim: "create a 3D object of a tree", "a red bucket", "Create a mini cartoon character", "Add a blonde wig on the head", "change the style to steampunk", "add lemons to the tree", "Make the character sit down and eat the ice cream with both hands". — [arXiv HTML 2508.08228](https://arxiv.org/html/2508.08228)
- LL3M README prompts, verbatim: "Create a 3D object of a chair"; "Create a 3D object of a stylish Chinese lantern with red silk material, golden trim, and intricate carved wooden frame"; "Create a 3D object of a cartoon blue fish with streamlined body, large expressive eyes, flowing tail fins"; refinement "add 2 armrests on the chair"; and "TERMINATE" to exit. The README warns that the LLM "may occasionally hallucinate or generate different results for the same prompt". — [GitHub threedle/ll3m](https://github.com/threedle/ll3m)
- LPM skill example brief, verbatim: "Low-poly Roman loot chest, 1.2 m, ≤ 2 500 tris, wood + bronze, collider, Unity/URP". Result: 868 triangles. — [blender-LPM-skill](https://github.com/ozanzeng/blender-LPM-skill)
- Stars as of 2026-09-27 via the GitHub API: threedle/ll3m 553; ozanzeng/blender-LPM-skill 4 (created 2026-08-28). — [ll3m](https://github.com/threedle/ll3m), [blender-LPM-skill](https://github.com/ozanzeng/blender-LPM-skill)

### Inferences
- LL3M's short user prompts worked because the system prompts and pipeline supplied the structure. A solo developer without that pipeline has to put the structure into the prompt or skill: the plan, part list, numbers, API version and review step. This is what the LPM skill does.
- LL3M's non-reproducibility warning ("different results for the same prompt") argues for the user's approach: commit the generated script as the asset source and treat the LLM as the author of a deterministic generator, not as the generator itself. The LPM skill likewise says "recipe script kept with the asset".

### Gaps
- Star counts for MeshCoder, BlenderLLM, BlenderAlchemy and SceneCraft were not retrieved.
- Citation counts were not retrieved.
- Verbatim prompts for SceneCraft, 3DCodeBench, ShapeCraft and Procedura showcase results were not retrieved; they are in the appendices or on the project pages.
