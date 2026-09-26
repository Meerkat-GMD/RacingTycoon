"""Rebuild documentation boards from the approved Blender renders.

This follows the existing preview scripts: resize/composite actual RGBA PNGs,
never paint or regenerate the characters. Unity assets and scenes are untouched.
Run from any directory: python Art/Blender/update_art_concept_boards.py
"""
from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont

ART = Path(__file__).resolve().parent
ROOT = ART.parents[1]
SCREENS = ROOT / 'docs/screenshots'
ARCHIVE = SCREENS / 'archive/2026-09-26'
PAPER = '#F5F0E7'
LIGHT = '#FFF6E7'
INK = '#29324D'
MUTED = '#737482'
ACCENT = '#58796F'
BOARDS = {'art-concept-lowpoly-test.png': (2060, 1220),
          'art-concept-vs-racing.png': (1658, 408)}
SOURCES = {
    'male': ART / 'GameCustomerFaceted/Customer_01_Faceted.png',
    'male_large': ART / 'GameCustomerFaceted/Customer_01_Faceted-large.png',
    'female': ART / 'GameCustomerFemaleExplorer/Customer_02_Explorer.png',
    'female_large': ART / 'GameCustomerFemaleExplorer/Customer_02_Explorer-large.png',
    'candy': ROOT / 'Assets/CottonCircuit/Sprites/Items/CottonCandy_Strawberry_Medium.png',
    'clock': ROOT / 'Assets/CottonCircuit/Sprites/Icons/Trait_Hours.png',
    'racing': SCREENS / 'urp-migration-racing.png',
}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def font(size, bold=False):
    for path in (Path('C:/Windows/Fonts') / ('malgunbd.ttf' if bold else 'malgun.ttf'),
                 Path('/usr/share/fonts/truetype/nanum') / ('NanumGothicBold.ttf' if bold else 'NanumGothic.ttf')):
        if path.is_file():
            return ImageFont.truetype(str(path), size)
    raise RuntimeError('A Korean font is required for the documentation labels')


def label(draw, xy, text, size=20, color=INK, bold=False, center=False):
    face = font(size, bold)
    x, y = xy
    if center:
        box = draw.textbbox((0, 0), text, font=face)
        x -= (box[2] - box[0]) / 2
    draw.text((x, y), text, font=face, fill=color)


def load_rgba(key, expected):
    with Image.open(SOURCES[key]) as source:
        source.load()
        assert source.mode == 'RGBA' and source.size == expected, (key, source.mode, source.size)
        image = source.copy()
    bounds = image.getchannel('A').getbbox()
    assert bounds and min(bounds[0], bounds[1], image.width-bounds[2], image.height-bounds[3]) >= 2
    return image


def place(board, sprite, xy):
    x, y = xy
    assert x >= 0 and y >= 0 and x+sprite.width <= board.width and y+sprite.height <= board.height
    board.alpha_composite(sprite, xy)


def native_panel(board, left, background, sprites):
    draw = ImageDraw.Draw(board)
    dark = background == INK
    color = LIGHT if dark else INK
    draw.rounded_rectangle((left, 958, left+970, 1188), radius=20, fill=background)
    label(draw, (left+24, 977), '실제 표시 크기 · ' + ('잉크 배경 #29324D' if dark else '밝은 배경 #FFF6E7'),
          21, color, True)
    entries = [('male', (82, 140), 220, '남성 82×140'),
               ('female', (82, 140), 362, '여성 82×140'),
               ('candy', (77, 88), 510, '솜사탕 77×88'),
               ('clock', (42, 42), 665, '시계 42×42')]
    checks = []
    for key, size, dx, caption in entries:
        sprite = sprites[key].resize(size, Image.Resampling.LANCZOS)
        x = left+dx
        y = 1150-size[1]
        place(board, sprite, (x, y))
        label(draw, (x+size[0]/2, 1157), caption, 16, color, center=True)
        checks.append({'key': key, 'size': size, 'xy': (x, y), 'background': background})
    return checks


def concept_board(sprites):
    board = Image.new('RGBA', BOARDS['art-concept-lowpoly-test.png'], PAPER)
    draw = ImageDraw.Draw(board)
    label(draw, (48, 24), 'RACING TYCOON  /  CHARACTER ART STANDARD  /  2026.09.27', 17, ACCENT, True)
    label(draw, (46, 53), 'A. 파스텔 토이 · 승인된 남녀 캐릭터', 39, bold=True)
    label(draw, (48, 113), '윤곽선 없는 플랫 셰이딩 · 공용 팔레트 · 작은 남색 점 눈 · 최신 Blender 원본 렌더', 21, MUTED)
    for x, key, title, caption in (
        (48, 'male_large', '남성 · 기본 손님', '각진 머리 다발 · 소다색 재킷 · 크로스백'),
        (714, 'female_large', '여성 · 탐험가 손님', '곡선형 머리 다발 · 크림색 모자 · 분홍 코트 · 배낭')):
        draw.rounded_rectangle((x, 166, x+642, 926), radius=24, fill=LIGHT)
        label(draw, (x+26, 184), title, 29, bold=True)
        label(draw, (x+26, 229), caption, 19, MUTED)
        place(board, sprites[key].resize((390, 666), Image.Resampling.LANCZOS), (x+126, 257))
    draw.rounded_rectangle((1380, 166, 2012, 926), radius=24, fill=LIGHT)
    label(draw, (1406, 184), '공통 소품 · 기존 샘플', 29, bold=True)
    label(draw, (1406, 229), '같은 팔레트와 조명으로 제작한 Blender 렌더', 19, MUTED)
    place(board, sprites['candy'].resize((308, 352), Image.Resampling.LANCZOS), (1542, 274))
    label(draw, (1696, 640), '딸기 맛 · 중간 크기 솜사탕', 23, bold=True, center=True)
    place(board, sprites['clock'].resize((168, 168), Image.Resampling.LANCZOS), (1612, 689))
    label(draw, (1696, 871), '벽시계 특성 아이콘', 23, bold=True, center=True)
    checks = native_panel(board, 48, LIGHT, sprites) + native_panel(board, 1042, INK, sprites)
    label(draw, (1030, 1194), '하단 이미지는 1× 배치입니다. 100% 배율에서 확인하세요.  |  캐릭터 PNG 164×280 → UI 82×140',
          16, MUTED, center=True)
    return board.convert('RGB'), checks


def racing_board(sprites):
    with Image.open(ARCHIVE / 'art-concept-vs-racing.png') as old:
        old = old.convert('RGB')
        assert old.size == BOARDS['art-concept-vs-racing.png']
        background = old.getpixel((1657, 407))
        preserved_left = old.crop((0, 0, 960, 408))
    board = Image.new('RGBA', old.size, (*background, 255))
    board.paste(preserved_left, (0, 0))
    draw = ImageDraw.Draw(board)
    for x, key, title in ((982, 'male_large', '남성 · 파스텔 토이'),
                          (1322, 'female_large', '여성 · 파스텔 토이')):
        label(draw, (x+6, 14), title, 26, bold=True)
        place(board, sprites[key].resize((197, 336), Image.Resampling.LANCZOS), (x+58, 60))
    return board.convert('RGB')


def verify(boards, native_checks, sprites):
    for name, expected in BOARDS.items():
        with Image.open(SCREENS / name) as saved:
            saved.load()
            assert saved.size == expected and saved.mode == 'RGB'
            assert ImageChops.difference(saved, boards[name]).getbbox() is None
    concept = boards['art-concept-lowpoly-test.png']
    for check in native_checks:
        key, size, (x, y), background = (check[k] for k in ('key', 'size', 'xy', 'background'))
        expected = Image.new('RGBA', size, background)
        expected.alpha_composite(sprites[key].resize(size, Image.Resampling.LANCZOS))
        actual = concept.crop((x, y, x+size[0], y+size[1]))
        assert ImageChops.difference(actual, expected.convert('RGB')).getbbox() is None, key
    with Image.open(SOURCES['racing']) as raw:
        racing_crop = raw.convert('RGB').crop((962, 200, 1920, 538))
    assert ImageChops.difference(boards['art-concept-vs-racing.png'].crop((0, 60, 958, 398)), racing_crop).getbbox() is None
    with Image.open(ARCHIVE / 'art-concept-vs-racing.png') as old:
        assert ImageChops.difference(boards['art-concept-vs-racing.png'].crop((0, 0, 960, 408)),
                                    old.convert('RGB').crop((0, 0, 960, 408))).getbbox() is None


def main():
    for name in BOARDS:
        if not (ARCHIVE / name).is_file():
            raise FileNotFoundError('Preserve the historical A/B board before replacing it: ' + str(ARCHIVE / name))
    hashes = {key: digest(path) for key, path in SOURCES.items()}
    sprites = {key: load_rgba(key, (656, 1120) if key.endswith('_large') else (164, 280))
               for key in ('male', 'male_large', 'female', 'female_large')}
    sprites['candy'] = load_rgba('candy', (154, 176))
    sprites['clock'] = load_rgba('clock', (84, 84))
    concept, checks = concept_board(sprites)
    boards = {'art-concept-lowpoly-test.png': concept, 'art-concept-vs-racing.png': racing_board(sprites)}
    for name, board in boards.items():
        board.save(SCREENS / name, optimize=True)
    verify(boards, checks, sprites)
    assert hashes == {key: digest(path) for key, path in SOURCES.items()}, 'A source asset changed'
    report = {'passed': True, 'date': '2026-09-27', 'method': 'Exact approved Blender PNGs, resized and alpha-composited only',
              'sources': {key: {'path': path.relative_to(ROOT).as_posix(), 'sha256': hashes[key]} for key, path in SOURCES.items()},
              'outputs': {name: {'size': list(size), 'sha256': digest(SCREENS/name)} for name, size in BOARDS.items()},
              'native_panels': checks, 'native_panels_match_source_pixels': True,
              'racing_pixels_unchanged': True, 'racing_source_crop': [962, 200, 1920, 538],
              'racing_board_destination': [0, 60, 958, 398], 'source_assets_unchanged': True}
    (ART / 'art-concept-boards-validation.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    for name, size in BOARDS.items():
        print(f'PASS {name}: {size[0]}x{size[1]}')
    print('PASS exact 1x native composites; original racing pixels and all source assets unchanged')


if __name__ == '__main__':
    main()
