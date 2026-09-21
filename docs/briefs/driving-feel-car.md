# Downhill coupe asset

The downhill mode uses an original pastel compact coupe with a low enclosed cabin, long hood, raised rear wing, and cream/soda body. Navy glazing, plum skirts, gold hubs, white headlights, and strawberry taillights retain Cotton Circuit's material palette. It has no text, logos, or licensed vehicle replica details.

The implementation follows the parent's driving-feel work and owns only the new coupe assets. The source has ground-centered origin, a Blender -Y nose, an approximately 1.55 × 3.10 × 1.05 m envelope, and four separate `WheelFL`, `WheelFR`, `WheelRL`, and `WheelRR` meshes with local X axles. Unity uses the same 180° Y visual wrapper as the existing kart.

Plan:

- [x] Inspect existing export, material, and wheel-animation conventions.
- [x] Build the deterministic Blender source and FBX with fewer than 15,000 triangles.
- [x] Validate source and FBX finite geometry, manifold outward faces, materials, bounds, wheel pivots, and nose direction.
- [x] Render and inspect the asset preview and record the results.

Rebuild with Blender 5.2:

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' -b -t 4 --python-exit-code 1 --python Art/Blender/create_downhill_coupe.py
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' -b -t 4 --python-exit-code 1 --python Art/Blender/validate_downhill_coupe.py
```

## Delivered assets and integration

- `Art/Blender/DownhillCoupe.blend`: editable source collection plus a separate render-only front/rear preview collection.
- `Art/Blender/create_downhill_coupe.py`: deterministic geometry, material, export, and preview generation.
- `Art/Blender/validate_downhill_coupe.py`: independent source and FBX contract validation.
- `Art/Blender/downhill-coupe-manifest.json`: object names, exact bounds, counts, palette, wheel centers, coordinate convention.
- `Art/Blender/downhill-coupe-preview.png`: 1600 × 1000 front/rear studio render, visually inspected.
- `Assets/CottonCircuit/Models/DownhillCoupe.fbx` and its initial `.meta`: import settings follow `Kart.fbx.meta`, including scale 1, baked axis conversion, readable meshes, and no imported cameras/lights/animation.

Actual bounds are X ±0.776 m, Y ±1.550 m, Z 0–1.050 m: **1.552 × 3.100 × 1.050 m**. The 2 mm width allowance is the outer hub detail. The asset has **50 meshes and 8,896 triangles**. All eight palette materials are used: Cream, Soda, Navy, Plum, Gold, Tire, White, and Strawberry.

Wheel pivots in Blender meters:

| Wheel | X | Y | Z |
|---|---:|---:|---:|
| WheelFL | -0.665 | -0.970 | 0.270 |
| WheelFR | 0.665 | -0.970 | 0.270 |
| WheelRL | -0.665 | 0.970 | 0.270 |
| WheelRR | 0.665 | 0.970 | 0.270 |

Hub geometry is joined into its wheel so the hub and tire rotate together. Preview instances, floor, lights, and camera are excluded from the FBX.

## Verification

Blender 5.2.2 LTS generated the source and export successfully. The validator initially failed because the requested source did not yet exist. After generation it exited 0, printing `VALID source`, `VALID FBX`, and `DOWNHILL_COUPE_VALIDATION_COMPLETE`.

Both stages checked finite vertices/transforms, nondegenerate faces, closed manifold edges with consistent winding, positive signed volume for every disconnected component, material assignment, matching triangle counts and bounds, ground origin, each wheel's name/pivot/local X axle, white headlights at -Y, strawberry taillights at +Y, navy glazing, and rear-wing placement. The render was inspected for readable front/rear silhouettes and visible wheel/wing/cabin details.

No Unity process was run and no gameplay, scene, existing asset, or GameAssets file was modified by this asset task. The parent task owns runtime material remapping and player/build verification.
