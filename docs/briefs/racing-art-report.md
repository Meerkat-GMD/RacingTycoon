# Racing props delivery

Generated with Blender 5.2.2 from `Art/Blender/create_racing_props.py`. Editable source is `Art/Blender/RacingProps.blend`; the three FBXs and stable Unity `.meta` files are under `Assets/CottonCircuit/Models`. The preview is `Art/Blender/racing-props-preview.png` and per-object bounds/materials are in `racing-props-manifest.json`.

| Prop | Blender dimensions (m) | Triangles | Visual read |
| --- | ---: | ---: | --- |
| Chevron | 2.50 × 0.31 × 1.99 | 1,248 | Navy board, three large yellow right chevrons |
| Barrier | 2.00 × 0.65 × 0.65 | 1,024 | Cream guardrail, candy wrappers and gold studs |
| ShortcutGate | 4.41 × 0.52 × 3.40 | 2,728 | 3.8 m clear opening, striped posts and gold bolt |

Each prop has a ground-centered origin. The modeled front faces Blender -Y. FBX export uses `axis_forward='-Z'`, `axis_up='Y'`, baked space transform, and the project's Unity importer rotation of 180 degrees around Y. Materials reuse the existing Strawberry, Cream, Soda, Vanilla, Navy, White, and Gold names.

Validation command:

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' -b -t 4 --python-exit-code 1 -P Art/Blender/validate_racing_props.py
```

Validation checks source meshes, finite geometry and normals, material names, ground contact, scale, FBX round-trip dimensions, gate clear width, and preview existence. The render was visually inspected: all three props are wholly visible and their direction, candy pattern, and bolt symbol read from the front. Unity runtime placement and scene appearance are owned by integration task 3.
