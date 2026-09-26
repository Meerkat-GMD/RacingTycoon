"""Validate the three initial Blender UI sprite samples with Pillow.

Run from any directory: python Art/Blender/validate_ui_sprites.py
The JSON report is written even when a source file is missing or invalid.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Art" / "Blender"
EXPECTED = {
    "CottonCandy_Strawberry_Medium": {
        "file": "Assets/CottonCircuit/Sprites/Items/CottonCandy_Strawberry_Medium.png",
        "display_size": [77, 88],
        "render_size": [154, 176],
        "shadow": True,
    },
    "Customer_01_Neutral": {
        "file": "Assets/CottonCircuit/Sprites/Customers/Customer_01_Neutral.png",
        "display_size": [82, 140],
        "render_size": [164, 280],
        "shadow": True,
    },
    "Trait_Hours": {
        "file": "Assets/CottonCircuit/Sprites/Icons/Trait_Hours.png",
        "display_size": [42, 42],
        "render_size": [84, 84],
        "shadow": False,
    },
}
EXPECTED_RENDER = {
    "engine": "CYCLES",
    "samples": 64,
    "denoise": True,
    "view_transform": "Standard",
    "look": "None",
}
APPROVED_COLORS = {
    "#F48DAB", "#7ACDCE", "#F9D27D", "#29324D", "#FFF1D4", "#FFF9ED",
    "#99C4AE", "#6C577F", "#DBAE61", "#D59C79", "#414059", "#9DBBAF",
    "#F1C6A6", "#B87A65", "#E8AF88", "#574F59", "#8D5B4A", "#45475B",
    "#7C789D", "#65688B", "#5F968F",
}


def pixel_data(image: Image.Image):
    """Prefer Pillow 12's replacement while supporting older Pillow versions."""
    return getattr(image, "get_flattened_data", image.getdata)()


def inspect_png(path: Path, expected: dict) -> tuple[dict, list[str], list[str]]:
    """Use rendered pixels only; metadata does not establish visual correctness."""
    failures: list[str] = []
    warnings: list[str] = []
    metrics: dict = {"file": path.relative_to(ROOT).as_posix(), "exists": path.is_file()}
    if not path.is_file():
        return metrics, ["PNG is missing"], warnings
    try:
        with Image.open(path) as source:
            source.load()
            metrics.update(format=source.format, mode=source.mode, size=list(source.size))
            if source.format != "PNG":
                failures.append("File is not a PNG")
            if source.mode != "RGBA":
                failures.append(f"PNG mode must be RGBA, got {source.mode}")
            if list(source.size) != expected["render_size"]:
                failures.append(f"Render size must be {expected['render_size']}, got {list(source.size)}")
            rgba = source.convert("RGBA")
    except (OSError, ValueError) as error:
        return metrics, [f"Cannot decode PNG: {error}"], warnings

    width, height = rgba.size
    alpha = rgba.getchannel("A")
    bounds = alpha.getbbox()
    metrics["alpha_bbox"] = list(bounds) if bounds else None
    metrics["alpha_extrema"] = list(alpha.getextrema())
    if bounds is None:
        failures.append("Sprite is entirely transparent")
        return metrics, failures, warnings
    left, top, right, bottom = bounds
    margins = {"left": left, "top": top, "right": width - right, "bottom": height - bottom}
    metrics["transparent_edge_pixels"] = margins
    if min(margins.values()) < 2:
        failures.append(f"At least 2 fully transparent edge pixels are required: {margins}")

    pixels = list(pixel_data(rgba))
    solid = [(r, g, b) for r, g, b, a in pixels if a >= 128]
    opaque = [(r, g, b) for r, g, b, a in pixels if a >= 250]
    occupancy = len(solid) / (width * height)
    metrics["solid_pixel_fraction"] = round(occupancy, 5)
    metrics["visible_pixel_fraction"] = round(sum(a > 0 for *_, a in pixels) / (width * height), 5)
    metrics["alpha_weighted_coverage"] = round(sum(a for *_, a in pixels) / (255 * width * height), 5)
    if not 0.06 <= occupancy <= 0.9:
        failures.append(f"Meaningful solid occupancy must be 6–90%, got {occupancy:.2%}")
    solid_bounds = alpha.point(lambda a: 255 if a >= 128 else 0).getbbox()
    metrics["solid_bbox"] = list(solid_bounds) if solid_bounds else None
    if solid_bounds:
        solid_width = solid_bounds[2] - solid_bounds[0]
        solid_height = solid_bounds[3] - solid_bounds[1]
        if solid_width < width * 0.25 or solid_height < height * 0.45:
            failures.append("Solid subject occupies too little of its canvas width or height")

    # A clipped red channel alone is expected for the approved cream and white.
    # Count all-channel neutral whites separately; lighting correctness still
    # requires inspecting the Blender source and the light/dark preview board.
    denominator = max(1, len(opaque))
    metrics["opaque_pixel_count"] = len(opaque)
    metrics["neutral_near_white_fraction"] = round(sum(min(rgb) >= 250 for rgb in opaque) / denominator, 6)
    metrics["neutral_clipped_white_fraction"] = round(sum(min(rgb) >= 254 for rgb in opaque) / denominator, 6)
    metrics["any_channel_255_fraction"] = round(sum(max(rgb) == 255 for rgb in opaque) / denominator, 6)
    metrics["opaque_unique_rgb_count"] = len(set(opaque))
    if metrics["neutral_near_white_fraction"] > 0.01:
        warnings.append("More than 1% of opaque pixels are neutral near-white; inspect highlights for lost facets")
    if opaque:
        luminance = [(0.2126 * r + 0.7152 * g + 0.0722 * b) / 255 for r, g, b in opaque]
        metrics["opaque_luminance_range"] = [round(min(luminance), 4), round(max(luminance), 4)]

    native = rgba.resize(tuple(expected["display_size"]), Image.Resampling.LANCZOS)
    metrics["native_solid_pixel_count"] = sum(a >= 128 for a in pixel_data(native.getchannel("A")))
    if expected["shadow"]:
        beauty_path = ART / "ui-sprite-passes" / f"{path.stem}-beauty.png"
        metrics["beauty_pass_comparison_available"] = beauty_path.is_file()
        if beauty_path.is_file():
            try:
                with Image.open(beauty_path) as beauty:
                    if beauty.mode != "RGBA" or beauty.size != rgba.size:
                        failures.append("Beauty pass must have the same RGBA mode and dimensions as the final sprite")
                    else:
                        pairs = [(a, b) for a, b in zip(pixel_data(beauty), pixels) if a[3] == 255]
                        metrics["beauty_opaque_pixel_count"] = len(pairs)
                        metrics["beauty_opaque_changed_pixel_count"] = sum(a != b for a, b in pairs)
                        metrics["beauty_opaque_max_channel_difference"] = max((abs(x - y) for a, b in pairs for x, y in zip(a, b)), default=0)
                        if metrics["beauty_opaque_changed_pixel_count"]:
                            failures.append("Shadow compositing changed opaque subject pixels from the original beauty pass")
            except (OSError, ValueError) as error:
                failures.append(f"Cannot decode beauty pass: {error}")
    if expected["display_size"] == [77, 88]:
        # An additional existing UI slot is shorter than the nominal sample size.
        scale = min(80 / width, 77 / height)
        metrics["optional_80x77_contain_size"] = [round(width * scale), round(height * scale)]
        metrics["optional_80x77_note"] = "Contain scaling preserves aspect ratio; slot integration is outside this sample task."
    return metrics, failures, warnings


def validate(manifest_path: Path) -> dict:
    report: dict = {
        "scope": "Three Blender UI sprite samples; visual readability is reviewed in the preview board.",
        "passed": False,
        "errors": [],
        "warnings": [],
        "assets": [],
    }
    blend = ART / "UiSprites.blend"
    report["blend"] = {"file": blend.relative_to(ROOT).as_posix(), "exists": blend.is_file()}
    if not blend.is_file():
        report["errors"].append("Editable source Art/Blender/UiSprites.blend is missing")
    elif blend.stat().st_size == 0:
        report["errors"].append("Editable Blender source is empty")
    else:
        report["blend"]["bytes"] = blend.stat().st_size
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    except (OSError, ValueError) as error:
        report["errors"].append(f"Cannot load manifest: {error}")
        return report
    if not isinstance(manifest, dict):
        report["errors"].append("Manifest root must be an object")
        return report

    render = manifest.get("render", {})
    for key, value in EXPECTED_RENDER.items():
        if not isinstance(render, dict) or render.get(key) != value:
            report["errors"].append(f"Manifest render.{key} must equal {value!r}")
    palette = manifest.get("palette", {})
    if not isinstance(palette, dict) or not palette:
        report["errors"].append("Manifest must record its named palette")
    else:
        for name, value in palette.items():
            if not isinstance(value, str) or value.upper() not in APPROVED_COLORS:
                report["errors"].append(f"Palette {name!r} has a color outside the approved spec: {value!r}")

    assets = manifest.get("assets", [])
    if not isinstance(assets, list):
        report["errors"].append("Manifest assets must be a list")
        return report
    ids = [asset.get("id") if isinstance(asset, dict) else None for asset in assets]
    report["manifest_asset_count"] = len(assets)
    if len(assets) != 3 or set(str(asset_id) for asset_id in ids) != set(EXPECTED):
        report["errors"].append(f"Expected exactly the three sample asset IDs, got {ids}")
    by_id = {asset.get("id"): asset for asset in assets if isinstance(asset, dict) and isinstance(asset.get("id"), str)}
    for asset_id, expected in EXPECTED.items():
        entry = by_id.get(asset_id, {})
        for key in ("file", "display_size", "render_size", "shadow"):
            if entry.get(key) != expected[key]:
                report["errors"].append(f"{asset_id}: manifest {key} must equal {expected[key]!r}")
        for key in ("scene", "collection"):
            if not isinstance(entry.get(key), str) or not entry.get(key):
                report["errors"].append(f"{asset_id}: manifest must name its Blender {key}")
        camera = entry.get("camera", {})
        if not isinstance(camera, dict) or not all(isinstance(camera.get(key), (int, float)) for key in ("yaw_deg", "elevation_deg", "orthographic_scale")):
            report["errors"].append(f"{asset_id}: manifest must record numeric camera yaw, elevation, and orthographic scale")
        elif camera["orthographic_scale"] <= 0:
            report["errors"].append(f"{asset_id}: orthographic scale must be positive")
        metrics, errors, warnings = inspect_png(ROOT / expected["file"], expected)
        report["assets"].append({"id": asset_id, "passed": not errors, "metrics": metrics, "errors": errors, "warnings": warnings})
        report["errors"].extend(f"{asset_id}: {message}" for message in errors)
        report["warnings"].extend(f"{asset_id}: {message}" for message in warnings)
    report["png_files_found"] = sum(asset["metrics"]["exists"] for asset in report["assets"])
    report["passed"] = not report["errors"]
    return report


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, default=ART / "ui-sprites-manifest.json")
    parser.add_argument("--output", type=Path, default=ART / "ui-sprites-validation.json")
    args = parser.parse_args()
    report = validate(args.manifest)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"UI sprite validation: {'PASS' if report['passed'] else 'FAIL'}")
    for message in report["errors"]:
        print(f"ERROR: {message}")
    for message in report["warnings"]:
        print(f"NOTE: {message}")
    for asset in report["assets"]:
        metrics = asset["metrics"]
        print(f"{asset['id']}: size={metrics.get('size')} edges={metrics.get('transparent_edge_pixels')} occupancy={metrics.get('solid_pixel_fraction')} neutral-near-white={metrics.get('neutral_near_white_fraction')}")
    print(f"Report: {args.output}")
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    sys.exit(main())
