"""Compare actual female eye revisions against the approved male head render."""

from PIL import Image, ImageDraw

from make_preview import ART, INK, LIGHT, MUTED, PAPER, centered_text, font, load, place


HEAD_CROP = (150, 70, 510, 415)
HEAD_DISPLAY = (420, 402)


def head(image: Image.Image) -> Image.Image:
    return image.crop(HEAD_CROP).resize(HEAD_DISPLAY, Image.Resampling.LANCZOS)


def main():
    before = load(ART / "revisions" / "eyes-v1" / "Customer_02_Explorer-large.png", (656, 1120))
    after = load(ART / "Customer_02_Explorer-large.png", (656, 1120))
    male = load(ART.parent / "GameCustomerFaceted" / "Customer_01_Faceted-large.png", (656, 1120))
    native = load(ART / "Customer_02_Explorer.png", (164, 280)).resize((82, 140), Image.Resampling.LANCZOS)

    board = Image.new("RGBA", (1448, 1000), PAPER)
    draw = ImageDraw.Draw(board)
    draw.text((32, 25), "COTTON CIRCUIT  /  EYE STYLE", fill="#58796F", font=font(16, True))
    draw.text((30, 55), "여성 탐험가 · 눈 스타일 비교", fill=INK, font=font(34, True))
    draw.text((32, 110), "수정 전후와 승인된 남성 캐릭터의 얼굴을 같은 영역과 배율로 비교합니다.", fill=MUTED, font=font(17))

    panels = (
        ("여성 · 수정 전", "기존 탐험가 눈", before),
        ("여성 · 수정 후", "남성 캐릭터와 같은 눈 스타일", after),
        ("승인된 남성", "눈 모양과 크기의 기준", male),
    )
    for index, (label, caption, image) in enumerate(panels):
        x = 32 + index * 468
        draw.rounded_rectangle((x, 158, x + 448, 670), radius=22, fill=LIGHT)
        draw.text((x + 22, 179), label, fill=INK, font=font(25, True))
        draw.text((x + 22, 219), caption, fill=MUTED, font=font(15))
        place(board, head(image), (x + 14, 253, x + 434, 657))

    centered_text(draw, 724, 700, "수정 후 여성 · 실제 표시 크기 82 × 140 px", 24, bold=True)
    centered_text(draw, 724, 738, "아래 스프라이트는 1x입니다. 100% 배율에서 표정을 확인하세요.", 16, MUTED)
    for x, background, label in ((394, LIGHT, "밝은 배경 · #FFF6E7"), (738, INK, "잉크 배경 · #29324D")):
        centered_text(draw, x + 158, 774, label, 16, bold=True)
        bounds = (x, 802, x + 316, 972)
        draw.rounded_rectangle(bounds, radius=16, fill=background)
        place(board, native, bounds)

    path = ART / "eyes-before-after.png"
    board.convert("RGB").save(path, optimize=True)
    print(f"Saved {path} (1448 x 1000)")
    print(f"All head panels share crop {HEAD_CROP} and display size {HEAD_DISPLAY}; native result is exactly 82 x 140.")


if __name__ == "__main__":
    main()
