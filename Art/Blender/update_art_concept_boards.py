"""Rebuild the two art-concept boards of the spec from the current Blender renders.

    python Art/Blender/update_art_concept_boards.py

Writes docs/screenshots/art-concept-lowpoly-test.png, docs/screenshots/art-concept-vs-racing.png
and Art/Blender/art-concept-boards-validation.json. Run from any directory.

- art-concept-lowpoly-test.png: the approved male (Customer_01_Faceted) and female explorer
  (Customer_02_Explorer), the child customer V1 beside them, and two game sprites from the
  same pipeline (CottonCandy_Strawberry_Medium 156x176, Trait_Hours 84x84). The top row
  shows the -large renders (656x1120) and the sprites at 2x; the bottom panels place every
  PNG at its 1x display size (82x140, 77x88, 42x42) on cream #FFF6E7 and ink #29324D.
- art-concept-vs-racing.png: the left 960 px of the archived board (the URP racing capture,
  kept pixel for pixel) next to the three customers.

Like the other preview scripts it only resizes and alpha-composites existing RGBA PNGs; it
never paints or re-renders the art. It checks that the 1x panels equal a plain composite of
the source pixels, that the racing pixels are unchanged and that no source changed while it
ran. Unity assets and scenes are not touched.
"""
from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont

ART = Path(__file__).resolve().parent
ROOT = ART.parents[1]
SCREENS = ROOT/'docs/screenshots'
ARCHIVE = SCREENS/'archive/2026-09-26'
SPRITES = ROOT/'Assets/CottonCircuit/Sprites'
PAPER = '#F5F0E7'
LIGHT = '#FFF6E7'
INK = '#29324D'
MUTED = '#737482'
ACCENT = '#58796F'
CONCEPT, RACING = 'art-concept-lowpoly-test.png', 'art-concept-vs-racing.png'
BOARDS = {CONCEPT: (2060, 1220), RACING: (1844, 408)}
SOURCES = {
    'male': ART/'GameCustomerFaceted/Customer_01_Faceted.png',
    'male_large': ART/'GameCustomerFaceted/Customer_01_Faceted-large.png',
    'female': ART/'GameCustomerFemaleExplorer/Customer_02_Explorer.png',
    'female_large': ART/'GameCustomerFemaleExplorer/Customer_02_Explorer-large.png',
    'child': SPRITES/'Customers/Customer_V1_Neutral.png',
    'child_large': ART/'GameCustomerChild/Customer_V1-large.png',
    'candy': SPRITES/'Items/CottonCandy_Strawberry_Medium.png',
    'clock': SPRITES/'Icons/Trait_Hours.png',
    'racing': SCREENS/'urp-migration-racing.png',
}
SIZES = {'male': (164, 280), 'female': (164, 280), 'child': (164, 280),
         'male_large': (656, 1120), 'female_large': (656, 1120), 'child_large': (656, 1120),
         'candy': (156, 176), 'clock': (84, 84)}
CHARACTERS = (  # key, board title, caption (concept board), racing-board title
    ('male', '남성 · 승인 손님', '각진 머리 다발 · 소다색 재킷 · 크로스백', '남성 손님'),
    ('female', '여성 · 승인 탐험가', '곡선형 머리 · 크림 모자 · 분홍 코트 · 배낭', '여성 탐험가'),
    ('child', '어린이 · 세 번째 손님', '쐐기 머리 · 딸기색 후드 · 반바지 · 지갑', '어린이 손님'),
)
NATIVE = (('male', (82, 140), '남성 82×140'), ('female', (82, 140), '여성 82×140'),
          ('child', (82, 140), '어린이 82×140'), ('candy', (77, 88), '솜사탕 77×88'),
          ('clock', (42, 42), '시계 42×42'))
MARGIN, GAP = 48, 24
CARD_TOP, CARD_BOTTOM = 166, 926
LARGE = (390, 666)                      # the 656x1120 renders at the scale of the old board
NATIVE_TOP, NATIVE_BOTTOM, NATIVE_BASE = 958, 1188, 1150
NATIVE_STEP = 142                       # slot pitch in the 1x panels
RACING_LEFT, RACING_COLUMN, RACING_LARGE = 960, 280, (197, 336)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def font(size, bold=False):
    for path in (Path('C:/Windows/Fonts')/('malgunbd.ttf' if bold else 'malgun.ttf'),
                 Path('/usr/share/fonts/truetype/nanum')/('NanumGothicBold.ttf' if bold else 'NanumGothic.ttf')):
        if path.is_file():
            return ImageFont.truetype(str(path), size)
    raise RuntimeError('A Korean font is required for the documentation labels')


def label(draw, xy, text, size=20, color=INK, bold=False, center=False, width=None):
    """Draw text (centred on x when center is set); when width is given, assert the text is no wider."""
    face = font(size, bold)
    box = draw.textbbox((0, 0), text, font=face)
    extent = box[2]-box[0]
    if width is not None:
        assert extent <= width, (text, extent, width)
    x, y = xy
    if center:
        x -= extent/2
    draw.text((x, y), text, font=face, fill=color)


def load_rgba(key):
    with Image.open(SOURCES[key]) as source:
        source.load()
        assert source.mode == 'RGBA' and source.size == SIZES[key], (key, source.mode, source.size)
        image = source.copy()
    bounds = image.getchannel('A').getbbox()
    assert bounds and min(bounds[0], bounds[1], image.width-bounds[2], image.height-bounds[3]) >= 2, key
    return image


def place(board, sprite, xy):
    x, y = xy
    assert x >= 0 and y >= 0 and x+sprite.width <= board.width and y+sprite.height <= board.height
    board.alpha_composite(sprite, xy)


def native_panel(board, left, width, background, sprites):
    draw = ImageDraw.Draw(board)
    dark = background == INK
    color = LIGHT if dark else INK
    draw.rounded_rectangle((left, NATIVE_TOP, left+width, NATIVE_BOTTOM), radius=20, fill=background)
    label(draw, (left+24, 977), '실제 표시 크기 · '+('잉크 배경 #29324D' if dark else '밝은 배경 #FFF6E7'),
          21, color, True, width=width-48)
    first = left+(width-NATIVE_STEP*(len(NATIVE)-1))//2   # slot centres, centred in the panel
    checks = []
    for index, (key, size, caption) in enumerate(NATIVE):
        centre = first+index*NATIVE_STEP
        sprite = sprites[key].resize(size, Image.Resampling.LANCZOS)
        x, y = centre-size[0]//2, NATIVE_BASE-size[1]
        place(board, sprite, (x, y))
        label(draw, (centre, 1157), caption, 16, color, center=True, width=NATIVE_STEP-8)
        checks.append({'key': key, 'size': size, 'xy': (x, y), 'background': background})
    return checks


def concept_board(sprites):
    board = Image.new('RGBA', BOARDS[CONCEPT], PAPER)
    draw = ImageDraw.Draw(board)
    label(draw, (48, 24), 'RACING TYCOON  /  CHARACTER ART STANDARD  /  2026.09.27', 17, ACCENT, True)
    label(draw, (46, 53), 'A. 파스텔 토이 · 손님 캐릭터와 게임 스프라이트', 39, bold=True)
    label(draw, (48, 113), '윤곽선 없는 플랫 셰이딩 · 공용 팔레트 · 작은 남색 점 눈 · 같은 조명과 카메라의 Blender 렌더',
          21, MUTED)
    card = (board.width-2*MARGIN-3*GAP)//4
    for index, (key, title, caption, _) in enumerate(CHARACTERS):
        x = MARGIN+index*(card+GAP)
        draw.rounded_rectangle((x, CARD_TOP, x+card, CARD_BOTTOM), radius=24, fill=LIGHT)
        label(draw, (x+26, 184), title, 29, bold=True, width=card-52)
        label(draw, (x+26, 229), caption, 18, MUTED, width=card-52)
        place(board, sprites[key+'_large'].resize(LARGE, Image.Resampling.LANCZOS),
              (x+(card-LARGE[0])//2, CARD_BOTTOM-3-LARGE[1]))
    x = MARGIN+3*(card+GAP)
    centre = x+card//2
    draw.rounded_rectangle((x, CARD_TOP, x+card, CARD_BOTTOM), radius=24, fill=LIGHT)
    label(draw, (x+26, 184), '공통 소품 · 게임 스프라이트', 29, bold=True, width=card-52)
    label(draw, (x+26, 229), '같은 팔레트·조명·재질로 만든 게임용 PNG', 18, MUTED, width=card-52)
    candy = sprites['candy'].resize((312, 352), Image.Resampling.LANCZOS)   # the 156x176 canvas at 2x
    place(board, candy, (centre-candy.width//2, 274))
    label(draw, (centre, 636), '딸기 맛 · 중간 크기 솜사탕', 23, bold=True, center=True, width=card-40)
    clock = sprites['clock'].resize((168, 168), Image.Resampling.LANCZOS)   # the 84x84 canvas at 2x
    place(board, clock, (centre-clock.width//2, 689))
    label(draw, (centre, 871), '벽시계 특성 아이콘', 23, bold=True, center=True, width=card-40)
    half = (board.width-2*MARGIN-GAP)//2
    checks = (native_panel(board, MARGIN, half, LIGHT, sprites)
              + native_panel(board, MARGIN+half+GAP, half, INK, sprites))
    label(draw, (board.width/2, 1194), '하단 이미지는 1× 배치입니다. 100% 배율에서 확인하세요.  |  '
          '캐릭터 PNG 164×280 → UI 82×140  |  솜사탕 156×176 → 77×88  |  시계 84×84 → 42×42', 16, MUTED, center=True,
          width=board.width-2*MARGIN)
    return board.convert('RGB'), checks


def racing_board(sprites):
    with Image.open(ARCHIVE/RACING) as old:
        old = old.convert('RGB')
        assert old.size == (1658, 408)
        background = old.getpixel((1657, 407))
        preserved_left = old.crop((0, 0, RACING_LEFT, 408))
    board = Image.new('RGBA', BOARDS[RACING], (*background, 255))
    board.paste(preserved_left, (0, 0))
    draw = ImageDraw.Draw(board)
    for index, (key, _, _, title) in enumerate(CHARACTERS):
        x = RACING_LEFT+22+index*RACING_COLUMN
        centre = x+RACING_COLUMN//2
        label(draw, (centre, 14), title, 26, bold=True, center=True, width=RACING_COLUMN-10)
        sprite = sprites[key+'_large'].resize(RACING_LARGE, Image.Resampling.LANCZOS)
        place(board, sprite, (centre-RACING_LARGE[0]//2, 60))
    assert x+RACING_COLUMN <= board.width
    return board.convert('RGB')


def verify(boards, native_checks, sprites):
    for name, expected in BOARDS.items():
        with Image.open(SCREENS/name) as saved:
            saved.load()
            assert saved.size == expected and saved.mode == 'RGB'
            assert ImageChops.difference(saved, boards[name]).getbbox() is None
    concept = boards[CONCEPT]
    for check in native_checks:
        key, size, (x, y), background = (check[k] for k in ('key', 'size', 'xy', 'background'))
        expected = Image.new('RGBA', size, background)
        expected.alpha_composite(sprites[key].resize(size, Image.Resampling.LANCZOS))
        actual = concept.crop((x, y, x+size[0], y+size[1]))
        assert ImageChops.difference(actual, expected.convert('RGB')).getbbox() is None, key
    with Image.open(SOURCES['racing']) as raw:
        racing_crop = raw.convert('RGB').crop((962, 200, 1920, 538))
    assert ImageChops.difference(boards[RACING].crop((0, 60, 958, 398)), racing_crop).getbbox() is None
    with Image.open(ARCHIVE/RACING) as old:
        assert ImageChops.difference(boards[RACING].crop((0, 0, RACING_LEFT, 408)),
                                    old.convert('RGB').crop((0, 0, RACING_LEFT, 408))).getbbox() is None


def main():
    for name in BOARDS:
        if not (ARCHIVE/name).is_file():
            raise FileNotFoundError('Preserve the historical A/B board before replacing it: '+str(ARCHIVE/name))
    hashes = {key: digest(path) for key, path in SOURCES.items()}
    sprites = {key: load_rgba(key) for key in SIZES}
    concept, checks = concept_board(sprites)
    boards = {CONCEPT: concept, RACING: racing_board(sprites)}
    for name, board in boards.items():
        board.save(SCREENS/name, optimize=True)
    verify(boards, checks, sprites)
    assert hashes == {key: digest(path) for key, path in SOURCES.items()}, 'A source asset changed'
    report = {'passed': True, 'date': '2026-09-27',
              'method': 'Existing Blender PNGs (approved male and female, child V1, game sprites), '
                        'resized and alpha-composited only',
              'sources': {key: {'path': path.relative_to(ROOT).as_posix(), 'sha256': hashes[key]}
                          for key, path in SOURCES.items()},
              'outputs': {name: {'size': list(size), 'sha256': digest(SCREENS/name)} for name, size in BOARDS.items()},
              'native_panels': checks, 'native_panels_match_source_pixels': True,
              'racing_pixels_unchanged': True, 'racing_source_crop': [962, 200, 1920, 538],
              'racing_board_destination': [0, 60, 958, 398], 'source_assets_unchanged': True}
    (ART/'art-concept-boards-validation.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n',
                                                          encoding='utf-8')
    for name, size in BOARDS.items():
        print(f'PASS {name}: {size[0]}x{size[1]}')
    print('PASS exact 1x native composites; original racing pixels and all source assets unchanged')


if __name__ == '__main__':
    main()
