# Noto Sans KR Regular for runtime UI

`NotoSansKR.ttf` is a static Regular (`wght=400`) instance of the official
[Google Fonts Noto Sans KR variable font](https://github.com/google/fonts/tree/main/ofl/notosanskr).
It retains the complete original character map and glyph set. The source variable
font defaults to Thin (`wght=100`); Unity's dynamic TextCore asset generator selected
that face, so a static instance is used to give normal UI text a true Regular outline.

- Original download: <https://raw.githubusercontent.com/google/fonts/main/ofl/notosanskr/NotoSansKR%5Bwght%5D.ttf>
- Original download retained at `Art/UI/Fonts/NotoSansKR-Variable.ttf`, outside Unity's Assets directory.
- License: SIL Open Font License 1.1, retained unchanged in `OFL.txt` beside the runtime font.
- Copyright: 2014–2021 Adobe; the license reserves the name **Source**, which is not used as this font's family name.
- Conversion: fontTools 4.63.0, `instantiateVariableFont(..., {"wght": 400}, updateFontNames=True, static=True)`.
- Source SHA-256: `194018e6b2b293a7964f037b25c0249ce1418bc9ab3c971060a03aa57861e252`
- Regular SHA-256: `f0bda2a63bebbf15f3c309e810144a801bb6ff3defa0258a3c5c302ca9fbd6cd`

Validation: no variable axes remain; OS/2 weight is 400; style name is Regular;
all 23,174 character-map entries and 24,964 glyphs are preserved. A sampled Korean
glyph has different outline coordinates from Thin, confirming this is a real weight
instance rather than a metadata rename or synthetic bold effect.

`CottonCircuit.Editor.ToolkitAssets.Ensure()` imports the source synchronously,
replaces an old non-Regular generated font through AssetDatabase, and requires
`font.faceInfo.styleName == "Regular"`. The next editor build emits
`COTTON_UI_FONT_REGULAR=True` with the resolved family and style. Generated Unity
font assets are rebuilt by the editor; they are not edited as YAML.
