# Map recipes and longer races implementation plan

> For agentic workers: use subagent-driven-development for independent core/course/art slices and requesting-code-review before release. User approved implementation; continue without another confirmation.

**Goal:** Map determines candy size, preparation determines flavor, one complete longer race determines the finished product and performance reward.
**Architecture:** Pure recipe-run API alongside existing GameSession legacy calls; two RaceCourse profiles feed existing ArcadeDrive; parent integrates Unity world, selection UI and smoke.
**Tech Stack:** Unity6000.5.3f1, built-in/uGUI, Blender5.2.2, Mono tests, Windows.

## Shared constraints
Spec: docs/superpowers/specs/2026-09-21-map-recipes-design.md. Baseline c20bf19; existing codex/cotton-circuit feature workspace. No other Unity projects/processes touched. No automatic sales. Old core APIs remain for legacy tests; real game uses recipe API. Map indices0/1; flavors0..2. Camera/shop/world coordinates and new UI handled by parent.

## Task1: recipe run/economy/persistence
Own Core/{RaceRecipe.cs(new),GameSession.cs,Production.cs,Economy.cs,CustomerOrders.cs}, SaveStore.cs, Tools/Tests/{RecipeTests.cs(new),OrderTests.cs}, Tools/test-recipes.ps1(new). Do not edit driving/scene/UI files.
- [x] RED tests: full target before lap staysRacing; selected flavor overrides incidental road; forward completed lap stocks correct size once; abort/timeout pays/stocksnothing; quality/bonusbounded; V1/V2 migrated by parent editor checks.
- [x] GREEN API: static RaceRecipe.TargetGrams(map)=60/120, Timeout(map)=180/240, ParSeconds(map)=50/75, Name(map) Korean name, Quality(boosts,hits,tankLevel) int0..100. CustomerOrder.Patience=300, adapt old waiting tests to constant.
- [x] Product adds public int Quality (0..100). Economy.Price multiplies oldbase*(1+Quality*.003), quality0legacyunchanged.
- [x] GameSession adds bool RecipeMode, int RecipeMap, RecipeFlavor, ResultBonus; double Elapsed; bool StartRecipe(int map,int flavor); void TickRecipe(double dt,double radians,double radius,int completedLaps,int boosts,int wallHits). Start validates; Production target60/120; radiansalreadyprogress-based winding amount. Tick ignores full as finish, timeout=>Results null/no reward, completedLaps>=1=>finish product(fill final missing samples fixedflavor),setQuality andcreditonecompletionbonus20/40maxbasedelapsedvspar. FinishRun whenRecipeMode aborts no product/reward. Legacy StartRun/Tick unchanged.
- [x] SaveV3: acceptV1/V2, oldquality0, V2 waitingRemaining*=300/120, retaincoins/stock/levels/tips. ValidateQuality0..100. CoreSaveStorecanworkwithoutUnitytests; parentextendsIntegrationChecks.
- [x] Run oldcore/order+newrecipe tests, commit owned and report docs/briefs/map-recipe-core-report.md.

## Task2: two course profiles and road meshes
Own Core/RaceCourse.cs, Editor/RaceCourseBuilder.cs, Tools/Tests/{DrivingTests.cs,MapTests.cs(new)}, Tools/test-maps.ps1(new).
- [x] RED tests for distinct loopslength750..850 /1050..1200, finiteboundsradius<=200, loopseams, roadprojection, allwaypointsandshortcutdrivable; actualbaseline18follower1lap45..90sec.
- [x] RaceCourse.ForMap(int map)0/1; Shared=>ForMap(0). Invalidindexthrows; Course API unchanged. Distinctlayouts ratherthanonlyscale, starterlongstraight, sweepingturns plus technicalbends. KeepconstMainHalfWidth4.8/ShortcutHalfWidth2.2; shortcutmapprogresscontinuous; expose public MapIndex and BoundsMin/Max RoadPoint forminimap ifuseful(parentcancomputepoints).
- [x] RaceCourseBuilder.Build constructs two rootTransforms assigned world.CourseRoots (parentaddsfield), eachwithitsowncourse. Runtimeworldtogglesroots. Names Racing surface 1/2 and Sugar cut shortcut 1/2. Entire road collects setflavor (noflavorstrip). ColorMap1InnerLane,Map2MiddleLane, shortcutRoadNeutral forvisualonly. Marker/rails/chevrons alongrealpath. Optional world.Assets.CandyTunnel,FinishMarker importedbyparent; placeusingtheseifnon-null (parentaddsfields). ExistingPlace/Cube/ReplaceAsset helperspreserved. Unique meshassetnames bymap. WorldcommonmachineRadius210 providedbyparent.
- [x] Adaptgeometry-specificoldtests toprofile coordinates/time while preservingbehaviorassertions (no weakenasserts). test-maps compilesallcore+MapTests; drivingtestsactualnewmap. Commitowned/report docs/briefs/map-course-report.md.

## Task3: Blender landmarks
Own Art/Blender/{MapLandmarks.blend,create_map_landmarks.py,validate_map_landmarks.py,map-landmarks-preview.png,map-landmarks-manifest.json}, Models/{CandyTunnel,FinishMarker}.fbx andinitialmetas.
- [x] BuildCandyTunnel openarch/internalclearancewidth11,height8,depth8m, pastelspiraltubes; FinishMarker decorativecandy/ribbon/podium marker width3,height5,depth1.5, placedroadside. Correct -Yfrontgroundorigin, semanticexistingmaterials. No textures required.
- [x] Validate source/FBXdimensions,normals,finitefaces/materials,renderpreview; commit/report docs/briefs/map-art-report.md.

## Task4: parent integration and verification
Own allremainingruntimeUI/Kart/World/GameAssets/ProjectBuilder/RaceMapGraphic/RuntimeSmoke/IntegrationChecks/docs.
- [x] Controller selectedMap/flavor defaultsfromorder; runtimeusesStartRecipe/TickRecipe; progress-basedall-laneproduction,one-lapcompletion; guardedabortconfirmation; one-time save/rewards.
- [x] UI map buttons,flavorbuttons,autoordersettings/racingfixedflavor+lapcompletion+quality;resultsbonus; retainstockselection/serve/shelf.
- [x] Worldtwoactivecourseroots; largercommonmachine; shopoffsetoutsidebowl; mapfitsdynamicminimap. Blenderimport15->17props. SetRaceFlavor colorssugarthread,candyalwaysselected; pathRadiusunchanged.
- [x] ExtendeditorchecksV2->V3quality/patience,modelrefs/twodistinctmaps. Rewriteplayersmoke6configuredrecipesontworoutes,fullproductnotearlyfinish,timeout/abort,bonusonce,manualserve,save,laneindependence,aspectlayout.
- [x] Core+maps+recipes tests; Blender validation; Unitydevelopmentbuild+player93equivalentrevisedchecks; independentreviewandfixes; renderreviewthenRelease. BuildfolderMapRecipes ifoldrunning; preserveuserprocess.
- [x] UpdateREADME/verification/ledger; commit reviewedwork. NoPRnecessarylocalproject.
