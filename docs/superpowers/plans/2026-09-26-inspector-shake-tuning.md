# Inspector sugar shake tuning

User clarified that shake width must be an authoring control in Unity Inspector, not a player setting. Replace the mistaken player preference implementation with a serialized GameController field.

- [x] Add serialized float `sugarShakeFullStrokePixels` to GameController, Range24–600, default132, Korean Inspector label and tooltip. Keep public normalized read API and pure-data OnValidate. Existing gesture loop reads it live. Remove player preference loading/saving; obsolete preference files never override Inspector values.
- [x] Remove the player shake settings panel and its added preparation button; restore compact pause/help UI. Delete unused persistence/UI source and standalone persistence tests.
- [x] Preserve the serialized value when the existing scene-generation build pipeline rebuilds the scene. Unity scene saving is the authority for authoring values; Play mode edits follow normal Unity revert behavior.
- [x] Verify private field through Unity SerializedObject and serialization roundtrip, live actual drag amount after field changes, pause UI absence, stale preference ignored, initialization/new shop preserving authoring value. Run build/runtime/core checks, review, update release and document exact Inspector location.

Hierarchy: `Cotton Circuit` → `Game Controller` → `설탕 흔들기` → `10g 흔들기 폭 (px)`. Changing during Play mode applies to subsequent gestures; stop Play mode and set/save the scene to keep the value permanently.

