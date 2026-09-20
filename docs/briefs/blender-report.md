# Cotton Circuit Blender asset kit report

Status: DONE.

Generated source and nine separate FBXs from `Art/Blender/create_assets.py` with Blender 5.2.2. The source `Art/Blender/CottonCircuit.blend` contains distinct Kart, Kiosk, Spinner, Puff, Customer, Crystal, Arch, Tree, Lamp, and Preview collections. The preview collection contains linked mesh duplicates and is excluded from individual FBX exports. `Art/Blender/asset-manifest.json` records material names and hex values, Blender-local bounds, dimensions, object names, and triangle counts. `Art/Blender/preview.png` is the rendered asset sheet. All geometry uses meters and Blender -Y as front; FBX export uses `axis_forward=-Z`, `axis_up=Y`, and baked space transform for Unity Y-up, +Z-front import. Only FBX files are placed under the Unity Models directory.

Validation command:

`C:/Program Files/Blender Foundation/Blender 5.2/blender.exe -b -t 4 -P Art/Blender/validate_assets.py`

Result: `VALIDATION_COMPLETE` with nine FBX imports. All exports have meshes, nonempty named material slots, nonzero bounds, and files above 10 KB. Kart includes WheelFL, WheelFR, WheelRL, WheelRR as distinct meshes, with cylinder pivots at their axle centers. Round-trip Blender import with matching `axis_forward=-Z`, `axis_up=Y` restored wheel centers at FL (-0.72, -0.78, 0.32), FR (0.72, -0.78, 0.32), RL (-0.72, 0.78, 0.32), RR (0.72, 0.78, 0.32). This verifies the front axle remains ahead on Blender -Y; the export basis maps Blender `(x, y, z)` to FBX/Unity `(x, z, -y)`, so that front direction becomes Unity +Z. Triangle counts: Kart 4246, Kiosk 4696, Spinner 2120, Puff 600, Customer 1400, Crystal 40, Arch 2592, Tree 1036, Lamp 762. The generator render completed and wrote a 1600 × 1000 PNG. Visual inspection of the actual preview confirmed material colors and legible kart, kiosk, spinner, arch, tree, customer, puff, and lamp silhouettes. Crystal is small in the full kit view but exists in the source and FBX.

Measured dimensions (Blender X, Y, Z meters): Kart 1.675 × 2.47 × 1.66; Kiosk 5 × 3 × 4.74; Spinner 1.95 × 1.95 × 6.59; Puff 1.05 × 0.936 × 0.891; Customer 0.917 × 0.505 × 1.65; Crystal 0.541 × 0.45 × 0.7; Arch 5 × 0.9 × 4.145; Tree 2.06 × 1.457 × 3.757; Lamp 0.54 × 0.54 × 3.42. The kart's body footprint is within the target silhouette, while its wheel extents make it about 7.5 cm wider than the nominal 1.6 m. The arch's structural posts sit at ±2.2 m and give 4.02 m nominal clear width between inner post faces; decorative stripes extend slightly inward. The spinner stick reaches 6.59 m including its rounded tip.

Integration notes: use the nine FBXs in `Assets/CottonCircuit/Models`, keeping their origins at the ground center except Puff, which is centered on its mesh. Mesh material names are semantic and have no textures. Imported Unity scale and facing should be checked in a scene since Blender's FBX importer round-trip does not exercise Unity's importer. No Unity editor was launched for this art task.
