# Customer-order shop art report

Created editable `Art/Blender/ShopProps.blend` with separate `DisplayRack`,
`OrderBoard`, and `QueuePost` collections. `create_shop_props.py` rebuilds the
source, three FBXs, manifest, and rendered preview. All parts remain independent
meshes with named pastel materials already used by the racing props.

| Prop | Blender dimensions (m) | Triangles | Description |
| --- | --- | ---: | --- |
| DisplayRack | 2.4 x 0.9 x 1.9 | 1404 | Two open shelves, front lips, header, and ground base |
| OrderBoard | 1.6 x 0.28 x 2.0 | 2800 | Three color and shape flavor symbols, no text |
| QueuePost | 0.4 x 0.4 x 1.0 | 1328 | Weighted base, stem, collar, and top cap |

The Blender front is -Y. FBX export uses `axis_forward='-Z'`, `axis_up='Y'`
as in the existing prop pipeline; the runtime can apply its established 180°
model rotation for Unity +Z front. The assets have centered ground origins.

Validation command:

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' -b -t 4 -P Art/Blender/validate_shop_props.py
```

Result: `SHOP_PROPS_VALIDATION_COMPLETE`. It checked source and FBX round-trip
dimensions, ground origins, material names, finite vertices, nondegenerate
faces, positive signed mesh volumes (outward normals), and -Y front cap normals
for every board symbol. The generated preview was visually inspected.

The three initial `.fbx.meta` files use the same Unity ModelImporter settings
as the racing props, with unique stable GUIDs.
