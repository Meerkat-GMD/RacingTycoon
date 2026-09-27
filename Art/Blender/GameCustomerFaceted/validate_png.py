"""Check the faceted customer PNGs independently of Blender scene metadata."""

from __future__ import annotations

import json
from pathlib import Path
import sys

from PIL import Image


ART = Path(__file__).resolve().parent
EXPECTED = {"Customer_01_Faceted.png": (164, 280), "Customer_01_Faceted-large.png": (656, 1120)}
BACKGROUNDS = {"cream": (255, 246, 231), "ink": (41, 50, 77)}


def pixels(image: Image.Image):
    return getattr(image, "get_flattened_data", image.getdata)()


def luminance(rgb):
    channels = [value / 255 for value in rgb]
    linear = [value / 12.92 if value <= 0.04045 else ((value + 0.055) / 1.055) ** 2.4 for value in channels]
    return sum(channel * weight for channel, weight in zip(linear, (0.2126, 0.7152, 0.0722)))


def contrast_metrics(image: Image.Image) -> dict:
    values = [luminance(pixel[:3]) for pixel in pixels(image) if pixel[3] >= 250]
    result = {}
    for name, rgb in BACKGROUNDS.items():
        bg = luminance(rgb)
        ratios = sorted((max(value, bg) + 0.05) / (min(value, bg) + 0.05) for value in values)
        result[name] = {
            "median_opaque_contrast_ratio": round(ratios[len(ratios) // 2], 3) if ratios else None,
            "opaque_fraction_contrast_at_least_1_5": round(sum(ratio >= 1.5 for ratio in ratios) / max(1, len(ratios)), 4),
        }
    return result


def inspect(path: Path, expected: tuple[int, int]) -> dict:
    result = {"file": path.name, "passed": False, "errors": [], "warnings": [], "metrics": {}}
    errors, metrics = result["errors"], result["metrics"]
    try:
        with Image.open(path) as source:
            source.load()
            metrics.update(format=source.format, mode=source.mode, size=list(source.size))
            if source.format != "PNG" or source.mode != "RGBA":
                errors.append("Expected an RGBA PNG")
            if source.size != expected:
                errors.append(f"Expected dimensions {expected}, got {source.size}")
            image = source.convert("RGBA")
    except (OSError, ValueError) as error:
        errors.append(f"Cannot decode render: {error}")
        return result

    width, height = image.size
    bounds = image.getchannel("A").getbbox()
    metrics["alpha_bbox"] = list(bounds) if bounds else None
    if not bounds:
        errors.append("Image contains no visible subject")
        return result
    left, top, right, bottom = bounds
    metrics["transparent_edge_pixels"] = {"left": left, "top": top, "right": width - right, "bottom": height - bottom}
    if min(metrics["transparent_edge_pixels"].values()) < 2:
        errors.append("The subject and its shadow need at least two fully transparent pixels at every edge")
    data = list(pixels(image))
    coverage = sum(pixel[3] >= 128 for pixel in data) / len(data)
    metrics["solid_pixel_fraction"] = round(coverage, 5)
    if not 0.08 <= coverage <= 0.85:
        errors.append(f"Subject occupancy must be 8–85%, got {coverage:.2%}")

    opaque = [pixel[:3] for pixel in data if pixel[3] >= 250]
    denominator = max(1, len(opaque))
    metrics["opaque_pixel_count"] = len(opaque)
    metrics["neutral_near_white_fraction"] = round(sum(min(rgb) >= 250 for rgb in opaque) / denominator, 6)
    metrics["neutral_clipped_white_fraction"] = round(sum(min(rgb) >= 254 for rgb in opaque) / denominator, 6)
    metrics["any_channel_255_fraction"] = round(sum(max(rgb) == 255 for rgb in opaque) / denominator, 6)
    metrics["opaque_unique_rgb_count"] = len(set(opaque))
    if metrics["neutral_near_white_fraction"] > 0.01:
        result["warnings"].append("Neutral near-white covers more than 1% of opaque pixels; visually inspect highlights for flattened facets")
    if len(set(opaque)) < 16:
        errors.append("Render has too few opaque colors to show the modeled facets")

    native = image.resize((82, 140), Image.Resampling.LANCZOS)
    metrics["native_display_size"] = [82, 140]
    metrics["native_solid_pixel_count"] = sum(alpha >= 128 for alpha in pixels(native.getchannel("A")))
    metrics["native_contrast"] = contrast_metrics(native)
    metrics["native_contrast_note"] = "Descriptive pixel metrics only; face readability and silhouette separation require visual review of customer-style-comparison.png at 100%."
    result["passed"] = not errors
    return result


def main() -> int:
    assets = [inspect(ART / name, size) for name, size in EXPECTED.items()]
    report = {"passed": all(asset["passed"] for asset in assets), "assets": assets}
    path = ART / "customer-png-validation.json"
    path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Customer PNG validation: {'PASS' if report['passed'] else 'FAIL'}")
    for asset in assets:
        print(f"{asset['file']}: {'PASS' if asset['passed'] else 'FAIL'}")
        for kind in ("errors", "warnings"):
            for message in asset[kind]:
                print(f"  {kind.upper()}: {message}")
        if asset["metrics"].get("transparent_edge_pixels"):
            print(f"  margins={asset['metrics']['transparent_edge_pixels']} neutral-near-white={asset['metrics']['neutral_near_white_fraction']} channel-255={asset['metrics']['any_channel_255_fraction']}")
    print(f"Report: {path}")
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    sys.exit(main())
