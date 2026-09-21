# Map landmark art report

Task 3 of `docs/superpowers/plans/2026-09-21-map-recipes.md` is complete.

## Assets

| Asset | Blender dimensions X / Y / Z | Meshes | Triangles |
| --- | --- | ---: | ---: |
| CandyTunnel | 12.92 / 8.00 / 8.96 m | 17 | 25,420 |
| FinishMarker | 3.00 / 1.50 / 4.992 m | 15 | 9,016 |

- `Art/Blender/MapLandmarks.blend` contains named editable mesh collections and a separate preview collection.
- `Art/Blender/create_map_landmarks.py` reproduces the source, two FBXs, manifest, and preview.
- `Art/Blender/validate_map_landmarks.py` checks both source geometry and freshly imported FBXs.
- `Art/Blender/map-landmarks-manifest.json` records bounds, semantic materials, object names, triangle counts, and tunnel clearance.
- `Art/Blender/map-landmarks-preview.png` is the visually inspected 1800 × 1100 render.
- `Assets/CottonCircuit/Models/CandyTunnel.fbx` and `FinishMarker.fbx` include initial model importer metas using the existing import settings and unique stable GUIDs.

CandyTunnel has three cream arches with strawberry, soda, and vanilla spiral stripes. It has no solid walls, roof, or floor. Slim side strings connect the arches outside the driving opening. Internal clearance is 11 m between the uprights and 8 m at the arch apex. The full 9.6 m roadway has at least 2.3 m headroom through its depth. Exterior depth is 8 m. The preview roadway is excluded from both FBX exports.

FinishMarker is a roadside podium with a spiral candy medallion, ribbon tails, and a small victory star. Its rounded star tip produces a measured 4.992 m height within the 5 m target. The front-facing swirl sits on Blender -Y. Both assets are centered at ground level, use meters, and use the project's seven existing semantic materials without textures. Export uses -Z forward / Y up; retain the existing Unity model wrapper's 180-degree Y rotation to make the visual front face Unity +Z.

## Validation

Ran Blender 5.2.2 LTS in background mode with four threads; no Unity process was started for this task.

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' -b -t 4 --python-exit-code 1 -P 'Art/Blender/create_map_landmarks.py'
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' -b -t 4 --python-exit-code 1 -P 'Art/Blender/validate_map_landmarks.py'
```

Generator exited 0 with `MAP_LANDMARKS_COMPLETE`. Validator exited 0 with `MAP_LANDMARKS_VALIDATION_COMPLETE` after both assets passed:

- Finite vertex coordinates and transforms; positive-area faces and unit face normals.
- Closed manifold edges and positive signed volume for every mesh.
- Ground-centered bounds, expected dimensions, and semantic material sets.
- Source versus FBX dimensions, mesh counts, material sets, and ground origins.
- Ray-tested tunnel clearance at multiple lateral positions and heights, including the inner arch contour, in source and reimported FBX.

The validation was first run before generation and failed on the missing source as expected. It subsequently caught bevel degeneracies in the thin ribbon highlights; reducing the bevel relative to thickness removed those faces without relaxing validation.

The rendered preview was inspected for clear road visibility, the open arch silhouette, the finish marker's front-facing candy and ribbons, and consistency with `shop-props-preview.png`. Unity placement, runtime checks, and material remapping remain in the parent/course integration tasks.
