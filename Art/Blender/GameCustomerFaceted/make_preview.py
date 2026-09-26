"""Compare real Blender customer renders; no model pixels are painted or retouched."""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ART = Path(__file__).resolve().parent
ROOT = ART.parents[2]
LIGHT = "#FFF6E7"
INK = "#29324D"
PAPER = "#F5F0E7"
MUTED = "#737482"
OLD = ROOT / "Assets/CottonCircuit/Sprites/Customers/Customer_01_Neutral.png"


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


def main():
    old = load(OLD, (164, 280))
    new = load(ART / "Customer_01_Faceted.png", (164, 280))
    large = load(ART / "Customer_01_Faceted-large.png", (656, 1120))

    board = Image.new("RGBA", (1200, 1000), PAPER)
    draw = ImageDraw.Draw(board)
    draw.text((32, 24), "COTTON CIRCUIT  /  CUSTOMER STUDY", fill="#58796F", font=font(16, True))
    draw.text((30, 53), "손님 캐릭터 · 패싯 스타일 비교", fill=INK, font=font(34, True))
    draw.text((32, 107), "새 모델의 형태와 기존 스프라이트를 실제 게임 표시 크기에서 비교합니다.", fill=MUTED, font=font(17))

    hero = large.resize((246, 420), Image.Resampling.LANCZOS)
    for x, background, foreground, caption in (
        (32, LIGHT, INK, "새 모델 / 밝은 배경"),
        (612, INK, LIGHT, "새 모델 / 잉크 배경"),
    ):
        draw.rounded_rectangle((x, 148, x + 556, 659), radius=22, fill=background)
        draw.text((x + 24, 170), caption, fill=foreground, font=font(20, True))
        draw.text((x + 24, 202), "3x UI · 고해상도 Blender 렌더", fill=foreground, font=font(14))
        place(board, hero, (x + 20, 232, x + 536, 652))

    draw.text((32, 690), "실제 표시 크기  /  82 × 140 px", fill=INK, font=font(23, True))
    draw.text((32, 727), "아래 네 캐릭터는 모두 1x입니다. 100% 배율에서 얼굴과 실루엣을 확인하세요.", fill=MUTED, font=font(16))
    tiles = (
        (old, LIGHT, "기존 · 밝은 배경"),
        (old, INK, "기존 · 잉크 배경"),
        (new, LIGHT, "새 모델 · 밝은 배경"),
        (new, INK, "새 모델 · 잉크 배경"),
    )
    for index, (sprite, background, label) in enumerate(tiles):
        x = 32 + index * 288
        centered_text(draw, x + 132, 767, label, 16, bold=True)
        bounds = (x, 799, x + 264, 969)
        draw.rounded_rectangle(bounds, radius=16, fill=background)
        place(board, sprite.resize((82, 140), Image.Resampling.LANCZOS), bounds)

    comparison_path = ART / "customer-style-comparison.png"
    board.convert("RGB").save(comparison_path, optimize=True)

    isolated = Image.new("RGBA", large.size, LIGHT)
    isolated.alpha_composite(large)
    isolated_path = ART / "Customer_01_Faceted-preview.png"
    isolated.convert("RGB").save(isolated_path, optimize=True)
    print(f"Saved {comparison_path} (1200 x 1000)")
    print(f"Saved {isolated_path} (656 x 1120)")


if __name__ == "__main__":
    main()
