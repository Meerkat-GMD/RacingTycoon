"""Compare the previous female and explorer Blender renders; no model pixels are painted or retouched."""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ART = Path(__file__).resolve().parent
ROOT = ART.parents[2]
LIGHT = "#FFF6E7"
INK = "#29324D"
PAPER = "#F5F0E7"
MUTED = "#737482"
PREVIOUS = ART.parent / "GameCustomerFemaleFaceted" / "Customer_02_Faceted.png"


def font(size: int, bold: bool = False):
    for path in (
        Path("C:/Windows/Fonts") / ("malgunbd.ttf" if bold else "malgun.ttf"),
        Path("C:/Windows/Fonts") / ("segoeuib.ttf" if bold else "segoeui.ttf"),
        Path("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf" if bold else "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"),
    ):
        if path.is_file():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default(size=size)


def load(path: Path, size: tuple[int, int]) -> Image.Image:
    with Image.open(path) as image:
        image.load()
        if image.format != "PNG" or image.mode != "RGBA" or image.size != size:
            raise ValueError(f"Expected an RGBA PNG at {size}: {path}")
        return image.copy()


def centered_text(draw, x: int, y: int, text: str, size: int, color: str = INK, bold: bool = False):
    face = font(size, bold)
    bounds = draw.textbbox((0, 0), text, font=face)
    draw.text((x - (bounds[2] - bounds[0]) / 2, y), text, font=face, fill=color)


def place(canvas: Image.Image, image: Image.Image, bounds: tuple[int, int, int, int]):
    left, top, right, bottom = bounds
    if image.width > right - left or image.height > bottom - top:
        raise ValueError("Image does not fit its preview panel")
    canvas.alpha_composite(image, (left + (right - left - image.width) // 2, top + (bottom - top - image.height) // 2))


def reference_comparison(large: Image.Image):
    reference_path = ART / "reference.png"
    if not reference_path.is_file():
        print("Optional reference-comparison.png skipped: reference.png is missing")
        return
    with Image.open(reference_path) as source:
        source.load()
        reference = source.convert("RGBA").crop((140, 205, 540, 1175))
    reference = reference.resize((round(reference.width * 800 / reference.height), 800), Image.Resampling.LANCZOS)

    # Crop only empty outer canvas for this presentation; preserve the complete
    # rendered subject and contact shadow, and do not paint or recolor any pixels.
    bounds = large.getchannel("A").getbbox()
    if not bounds:
        raise ValueError("Explorer render is entirely transparent")
    model = large.crop(bounds)
    model = model.resize((round(model.width * 800 / model.height), 800), Image.Resampling.LANCZOS)

    board = Image.new("RGBA", (1060, 1104), PAPER)
    draw = ImageDraw.Draw(board)
    draw.text((32, 25), "COTTON CIRCUIT  /  REFERENCE TO BLENDER", fill="#58796F", font=font(16, True))
    draw.text((30, 58), "탐험가 디자인 · 참고 이미지 비교", fill=INK, font=font(31, True))
    draw.text((32, 108), "모자, 표정, 스카프와 배낭의 특징을 게임용 로우폴리 모델에 적용했습니다.", fill=MUTED, font=font(16))
    for x, label, caption, image in (
        (32, "참고 이미지", "인물 중심으로 일부 크롭", reference),
        (542, "Blender 재제작", "모델 렌더 · 크기 조정만 적용", model),
    ):
        draw.rounded_rectangle((x, 155, x + 486, 1072), radius=22, fill=LIGHT)
        draw.text((x + 24, 175), label, fill=INK, font=font(24, True))
        draw.text((x + 24, 211), caption, fill=MUTED, font=font(14))
        place(board, image, (x + 16, 246, x + 470, 1054))
    path = ART / "reference-comparison.png"
    board.convert("RGB").save(path, optimize=True)
    print(f"Saved {path} (1060 x 1104)")


def main():
    previous = load(PREVIOUS, (164, 280))
    female = load(ART / "Customer_02_Explorer.png", (164, 280))
    large = load(ART / "Customer_02_Explorer-large.png", (656, 1120))

    board = Image.new("RGBA", (1200, 1000), PAPER)
    draw = ImageDraw.Draw(board)
    draw.text((32, 24), "COTTON CIRCUIT  /  EXPLORER STUDY", fill="#58796F", font=font(16, True))
    draw.text((30, 53), "여성 손님 · 탐험가 스타일 비교", fill=INK, font=font(34, True))
    draw.text((32, 107), "이전 디자인과 새 탐험가 디자인을 실제 게임 표시 크기에서 비교합니다.", fill=MUTED, font=font(17))

    hero = large.resize((246, 420), Image.Resampling.LANCZOS)
    for x, background, foreground, caption in (
        (32, LIGHT, INK, "탐험가 디자인 / 밝은 배경"),
        (612, INK, LIGHT, "탐험가 디자인 / 잉크 배경"),
    ):
        draw.rounded_rectangle((x, 148, x + 556, 659), radius=22, fill=background)
        draw.text((x + 24, 170), caption, fill=foreground, font=font(20, True))
        draw.text((x + 24, 202), "3x UI · 고해상도 Blender 렌더", fill=foreground, font=font(14))
        place(board, hero, (x + 20, 232, x + 536, 652))

    draw.text((32, 690), "실제 표시 크기  /  82 × 140 px", fill=INK, font=font(23, True))
    draw.text((32, 727), "아래 네 캐릭터는 모두 1x입니다. 100% 배율에서 얼굴과 실루엣을 확인하세요.", fill=MUTED, font=font(16))
    tiles = (
        (previous, LIGHT, "이전 여성 · 밝은 배경"),
        (previous, INK, "이전 여성 · 잉크 배경"),
        (female, LIGHT, "탐험가 · 밝은 배경"),
        (female, INK, "탐험가 · 잉크 배경"),
    )
    for index, (sprite, background, label) in enumerate(tiles):
        x = 32 + index * 288
        centered_text(draw, x + 132, 767, label, 16, bold=True)
        bounds = (x, 799, x + 264, 969)
        draw.rounded_rectangle(bounds, radius=16, fill=background)
        place(board, sprite.resize((82, 140), Image.Resampling.LANCZOS), bounds)

    comparison_path = ART / "female-explorer-comparison.png"
    board.convert("RGB").save(comparison_path, optimize=True)

    isolated = Image.new("RGBA", large.size, LIGHT)
    isolated.alpha_composite(large)
    isolated_path = ART / "Customer_02_Explorer-preview.png"
    isolated.convert("RGB").save(isolated_path, optimize=True)
    print(f"Saved {comparison_path} (1200 x 1000)")
    print(f"Saved {isolated_path} (656 x 1120)")
    reference_comparison(large)


if __name__ == "__main__":
    main()
