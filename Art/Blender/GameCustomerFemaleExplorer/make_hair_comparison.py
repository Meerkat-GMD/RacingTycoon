"""Compare actual hair revision renders with identical crops and display scales."""

from pathlib import Path

from PIL import Image, ImageDraw

from make_preview import ART, INK, LIGHT, MUTED, PAPER, font, load, place


HEAD_CROP = (150, 70, 510, 415)
HEAD_DISPLAY = (420, 402)


def main():
    before_path = ART / "revisions" / "hair-v1" / "Customer_02_Explorer-large.png"
    after_path = ART / "Customer_02_Explorer-large.png"
    before = load(before_path, (656, 1120)).crop(HEAD_CROP).resize(HEAD_DISPLAY, Image.Resampling.LANCZOS)
    after = load(after_path, (656, 1120)).crop(HEAD_CROP).resize(HEAD_DISPLAY, Image.Resampling.LANCZOS)
    panels = [
        ("수정 전", "기존 헤어 · 동일 위치 확대", before),
        ("수정 후", "개선된 헤어 · 동일 위치 확대", after),
    ]
    reference_path = ART / "reference.png"
    if reference_path.is_file():
        with Image.open(reference_path) as source:
            source.load()
            reference = source.convert("RGBA").crop((165, 195, 510, 475))
        reference = reference.resize((420, round(reference.height * 420 / reference.width)), Image.Resampling.LANCZOS)
        panels.append(("참고 이미지", "헤어 흐름과 귀 주변 형태", reference))

    width = 64 + len(panels) * 448 + (len(panels) - 1) * 20
    board = Image.new("RGBA", (width, 724), PAPER)
    draw = ImageDraw.Draw(board)
    draw.text((32, 25), "COTTON CIRCUIT  /  HAIR REFINEMENT", fill="#58796F", font=font(16, True))
    draw.text((30, 55), "탐험가 헤어 · 수정 전후", fill=INK, font=font(34, True))
    draw.text((32, 110), "수정 전후 렌더의 같은 영역을 동일 배율로 확대했습니다.", fill=MUTED, font=font(17))
    for index, (label, caption, image) in enumerate(panels):
        x = 32 + index * 468
        draw.rounded_rectangle((x, 158, x + 448, 700), radius=22, fill=LIGHT)
        draw.text((x + 22, 179), label, fill=INK, font=font(25, True))
        draw.text((x + 22, 219), caption, fill=MUTED, font=font(15))
        place(board, image, (x + 14, 270, x + 434, 686))

    path = ART / "hair-before-after.png"
    board.convert("RGB").save(path, optimize=True)
    print(f"Saved {path} ({width} x 724)")
    print(f"Before/after share crop {HEAD_CROP} and display size {HEAD_DISPLAY}; no model pixels retouched.")


if __name__ == "__main__":
    main()
