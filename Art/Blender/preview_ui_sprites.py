"""Compose Blender-rendered samples at 4x and exact 1x UI size.

This script only resizes and composites existing PNG renders; it creates no art.
Run after create_ui_sprites.py with: python Art/Blender/preview_ui_sprites.py
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from validate_ui_sprites import ART, EXPECTED, ROOT


LIGHT = "#FFF6E7"
DARK = "#29324D"
PAPER = "#F5F0E7"
INK = "#29324D"
MUTED = "#737482"
RULE = "#E5DED1"
LABELS = {
    "CottonCandy_Strawberry_Medium": ("Strawberry cotton candy", "PRODUCT  /  MEDIUM"),
    "Customer_01_Neutral": ("Neighborhood customer", "CHARACTER  /  NEUTRAL"),
    "Trait_Hours": ("Opening hours", "TRAIT ICON  /  HOURS"),
}


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = [
        Path("C:/Windows/Fonts") / ("segoeuib.ttf" if bold else "segoeui.ttf"),
        Path("C:/Windows/Fonts") / ("arialbd.ttf" if bold else "arial.ttf"),
        Path("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf" if bold else "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"),
    ]
    for candidate in candidates:
        if candidate.is_file():
            return ImageFont.truetype(str(candidate), size)
    return ImageFont.load_default(size=size)


def centered_text(draw: ImageDraw.ImageDraw, x: int, y: int, text: str, size: int, fill: str = INK, bold: bool = False) -> None:
    face = font(size, bold)
    box = draw.textbbox((0, 0), text, font=face)
    draw.text((x - (box[2] - box[0]) / 2, y), text, font=face, fill=fill)


def place(canvas: Image.Image, sprite: Image.Image, box: tuple[int, int, int, int]) -> None:
    left, top, right, bottom = box
    if sprite.width > right - left or sprite.height > bottom - top:
        raise ValueError(f"Preview box {box} cannot contain {sprite.size}")
    canvas.alpha_composite(sprite, (left + (right - left - sprite.width) // 2, top + (bottom - top - sprite.height) // 2))


def load_assets(manifest_path: Path) -> list[dict]:
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    entries = manifest.get("assets", [])
    if len(entries) != 3 or {entry["id"] for entry in entries} != set(EXPECTED):
        raise ValueError("Preview requires exactly the three initial sample assets")
    by_id = {entry["id"]: entry for entry in entries}
    assets = []
    for asset_id, expected in EXPECTED.items():
        entry = by_id[asset_id]
        if any(entry[key] != expected[key] for key in ("file", "display_size", "render_size")):
            raise ValueError(f"{asset_id}: manifest sizes or output path do not match the sample contract")
        with Image.open(ROOT / expected["file"]) as source:
            source.load()
            if source.mode != "RGBA" or list(source.size) != expected["render_size"]:
                raise ValueError(f"{asset_id}: preview requires an RGBA PNG of {expected['render_size']}")
            sprite = source.copy()
        assets.append({"id": asset_id, "image": sprite, **expected})
    return assets


def comparison_board(assets: list[dict]) -> Image.Image:
    canvas = Image.new("RGBA", (1380, 1184), PAPER)
    draw = ImageDraw.Draw(canvas)
    draw.text((42, 28), "COTTON CIRCUIT  /  BLENDER ART STUDY", fill="#58796F", font=font(17, True))
    draw.text((40, 56), "Pastel toys, ready for the UI", fill=INK, font=font(39, True))
    draw.text((42, 113), "Three sample renders  |  Flat shaded geometry  |  Transparent RGBA", fill=MUTED, font=font(18))
    draw.line((42, 151, 1338, 151), fill=RULE, width=2)

    for index, asset in enumerate(assets):
        x = 42 + index * 444
        title, kicker = LABELS[asset["id"]]
        sprite = asset["image"]
        draw.rounded_rectangle((x, 174, x + 408, 817), radius=20, fill=LIGHT)
        draw.text((x + 22, 195), kicker, fill="#8C8074", font=font(13, True))
        draw.text((x + 22, 218), title, fill=INK, font=font(22, True))
        enlarged = sprite.resize((sprite.width * 2, sprite.height * 2), Image.Resampling.NEAREST)
        place(canvas, enlarged, (x + 16, 252, x + 392, 817))

        width, height = asset["display_size"]
        centered_text(draw, x + 204, 836, f"{width} x {height} px at 1x UI", 19, bold=True)
        native = sprite.resize((width, height), Image.Resampling.LANCZOS)
        for offset, background, caption in ((0, LIGHT, "LIGHT / #FFF6E7"), (210, DARK, "INK / #29324D")):
            box = (x + offset, 875, x + offset + 198, 1077)
            draw.rounded_rectangle(box, radius=16, fill=background)
            place(canvas, native, box)
            centered_text(draw, x + offset + 99, 1088, caption, 13, MUTED)

    draw.line((42, 1122, 1338, 1122), fill=RULE, width=2)
    draw.text((42, 1138), "Top: PNG enlarged 2x (4x UI). Bottom: exact 1x UI. Native render resolution is 2x UI.", fill=MUTED, font=font(16))
    return canvas.convert("RGB")


def native_board(assets: list[dict]) -> Image.Image:
    canvas = Image.new("RGBA", (816, 304), PAPER)
    draw = ImageDraw.Draw(canvas)
    draw.text((20, 13), "COTTON CIRCUIT  /  EXACT 1x UI", fill=INK, font=font(20, True))
    draw.text((20, 44), "Light and ink backgrounds. View at 100% for native-size review.", fill=MUTED, font=font(14))
    for index, asset in enumerate(assets):
        x = 20 + index * 264
        width, height = asset["display_size"]
        image = asset["image"].resize((width, height), Image.Resampling.LANCZOS)
        for offset, color in ((0, LIGHT), (122, DARK)):
            box = (x + offset, 79, x + offset + 116, 249)
            draw.rounded_rectangle(box, radius=12, fill=color)
            place(canvas, image, box)
        short_title = ("Cotton candy", "Customer", "Hours icon")[index]
        centered_text(draw, x + 119, 261, f"{short_title}  /  {width} x {height} px", 15, bold=True)
    return canvas.convert("RGB")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, default=ART / "ui-sprites-manifest.json")
    args = parser.parse_args()
    assets = load_assets(args.manifest)
    for name, board in (("ui-sprites-preview.png", comparison_board(assets)), ("ui-sprites-native.png", native_board(assets))):
        path = ART / name
        board.save(path, optimize=True)
        print(f"Saved {path} ({board.width} x {board.height})")


if __name__ == "__main__":
    main()
