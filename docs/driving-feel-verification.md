# Driving style verification — 2026-09-21

Environment: Windows / Unity6000.5.3f1 / Blender5.2.2LTS. Baseline2f6ad73.

## Verified pure behavior

- Core22, Driving17, Orders14, Recipes17, Feel21, Maps12 passed in fresh parent runs.
- Kart base26m/s; >=23.4m/s within0.8seconds. Weak/strong immediate boosts, speed caps, smooth expiry and brake/recovery/wall cancellation pass. Repeated boost cannot stack past its cap.
- Downhill uses greater steering/velocity inertia, allows brake-entry slides, never releases Kart boosts, and completes slides into SkillCount. Stationary/straight input does not generate skills; a slide released into collision earns no clean-slide point.
- Both modes preserve earned-progress/anti-oscillation/shortcut/recovery protections. Recipes retain map-selected60/120g, selected flavor, one forward lap completion, no product or bonus on abort/timeout, manual sales and V3 save migration.
- Course1 length560.81m; Kart22.28s, Downhill22.50s. Course2 length668.62m; Kart26.44s, Downhill26.66s. Both clean and full progress in pure simulation. Shortcut saves13.0m/16.6m and both modes actually traverse/finish it. Test anticipation12mKart/14mDownhill accounts for speed and inertia; production is manually steered.

## Unity and art

- Development build succeeded, editor55 checks passed. Includes actualJSON persistence, all18Blenderassets, selectedcourse roots, roadsidepropcounts and renderer-versus-drivable-lane clearance.
- New coupe source and reimportedFBX validator passed:50meshes8896triangles, dimensions1.552×3.100×1.050m, finite/nondegenerate/manifold/outward geometry, materials, localXwheelpivots, forwardorientation. Blender source and generated preview inspected.
- Windows smoke188/188 passed after evidence-based Downhill test-driver anticipation16m with0.8m variation. Bothstylecourses0hits, Kart22.34/26.46s andDownhill22.40/26.52s. Boost/slides/quality, pause/mute, style/vehicle switching, UIresolutions, sales/save pass. Finaltoastposition visualconfirmation follows the passingrun; see verification.md.

## Scope

Two selectable driving styles on the same two flat courses for direct comparison. Kart uses the existing kart and drift-release boosts. Downhill uses the new coupe and a lower camera with more inertia; it is not yet a mountainous/downhill terrain map. No imitation franchise assets or logos. Style choice is retained within the running game and does not alter saved economy format.

Target release Builds/DrivingFeel/CottonCircuit.exe. Existing old Build/Windows player remains untouched. Human handling preference is not certified by automated driving tests.
