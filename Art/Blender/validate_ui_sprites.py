"""Validate the pastel-toy UI sprites against the catalog in ui_sprite_spec.py (Pillow + numpy).

    python Art/Blender/validate_ui_sprites.py [--complete] [--owner NAME[,NAME...]] [--report PATH]

For every catalog entry whose PNG exists: RGBA, size == canvas and the kind rules
(transparent kinds keep >= margin fully transparent pixels on every edge and an
alpha >= 128 occupancy inside the kind's range; opaque kinds are alpha 255
everywhere). Shadowed sprites whose Art/Blender/ui-sprite-passes/<owner>/<id>-beauty.png
exists must keep every opaque beauty pixel unchanged. Every
Art/Blender/ui_sprites/*.manifest.json must match the shared render settings,
palette and material recipe. PNGs under the sprite folder that are not in the
catalog fail. --complete also fails for every missing PNG.
--owner NAME (repeatable, or comma-separated) restricts every check above to the
catalog entries owned by those owners (ui_sprite_spec.owned_by), so one art task's
unfinished sprites cannot fail another task's validation run; an unknown owner
exits 2. Prints `UI_SPRITES_VALID <present>/66` (or `.../<owned count> owner=<names>`
when --owner is given) or the failures; exits 1 on failure.
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ART = Path(__file__).resolve().parent
ROOT = ART.parents[1]
if str(ART) not in sys.path:
    sys.path.insert(0, str(ART))
import ui_sprite_spec as spec

SPRITES = 'Assets/CottonCircuit/Sprites'
PASSES = 'Art/Blender/ui-sprite-passes'
MANIFESTS = 'Art/Blender/ui_sprites'
PALETTE_HEX = {value.upper() for value in spec.PALETTE.values()}
OWNERS = sorted({entry['owner'] for entry in spec.CATALOG})


def load_rgba(path: Path) -> tuple[str, tuple[int, int], np.ndarray]:
    with Image.open(path) as image:
        image.load()
        return image.mode, image.size, np.asarray(image.convert('RGBA'), dtype=np.uint8)


def check_sprite(entry: dict, root: Path) -> dict:
    """Pixel checks for one catalog entry; metadata alone never passes a sprite."""
    path = root/spec.sprite_path(entry)
    result = {'id': entry['id'], 'file': spec.sprite_path(entry), 'kind': entry['kind'],
              'exists': path.is_file(), 'errors': [], 'warnings': [], 'metrics': {}}
    if not result['exists']:
        return result
    errors, metrics = result['errors'], result['metrics']
    try:
        mode, size, rgba = load_rgba(path)
    except (OSError, ValueError) as error:
        errors.append('cannot decode PNG: %s' % error)
        return result
    metrics.update(mode=mode, size=list(size))
    if mode != 'RGBA':
        errors.append('mode must be RGBA, got %s' % mode)
    if tuple(size) != tuple(entry['canvas']):
        errors.append('size must be %dx%d, got %dx%d' % (*entry['canvas'], *size))
        return result
    rules = spec.KIND_RULES[entry['kind']]
    alpha = rgba[..., 3]
    if rules['opaque']:
        metrics['min_alpha'] = int(alpha.min())
        if metrics['min_alpha'] != 255:
            errors.append('opaque kind needs alpha 255 everywhere; %d pixels are not' % int((alpha != 255).sum()))
        return result
    height, width = alpha.shape
    rows, cols = np.nonzero(alpha)
    if rows.size == 0:
        errors.append('sprite is entirely transparent')
        return result
    margins = {'left': int(cols.min()), 'top': int(rows.min()),
               'right': int(width-1-cols.max()), 'bottom': int(height-1-rows.max())}
    metrics['transparent_margins'] = margins
    if min(margins.values()) < rules['margin']:
        errors.append('needs >= %d fully transparent px on every edge, got %s' % (rules['margin'], margins))
    occupancy = float((alpha >= 128).mean())
    metrics['occupancy'] = round(occupancy, 5)
    low, high = rules['occupancy']
    if not low <= occupancy <= high:
        errors.append('alpha>=128 occupancy must be %.0f-%.0f%%, got %.2f%%' % (low*100, high*100, occupancy*100))
    opaque = rgba[alpha >= 250][:, :3]
    if len(opaque):
        near_white = float((opaque.min(axis=1) >= 250).mean())
        metrics['neutral_near_white_fraction'] = round(near_white, 6)
        if near_white > .01:
            result['warnings'].append('%.1f%% of opaque pixels are neutral near-white; check for clipped facets'
                                      % (near_white*100))
    if entry['shadow']:
        beauty_path = root/PASSES/entry['owner']/(entry['id']+'-beauty.png')
        metrics['beauty_pass'] = beauty_path.is_file()
        if beauty_path.is_file():
            try:
                _, beauty_size, beauty = load_rgba(beauty_path)
            except (OSError, ValueError) as error:
                errors.append('cannot decode beauty pass: %s' % error)
                return result
            if beauty_size != size:
                errors.append('beauty pass is %dx%d, sprite is %dx%d' % (*beauty_size, *size))
            else:
                solid = beauty[..., 3] == 255
                changed = int((beauty[solid] != rgba[solid]).any(axis=1).sum())
                metrics['beauty_opaque_pixels'] = int(solid.sum())
                metrics['beauty_opaque_changed'] = changed
                if changed:
                    errors.append('shadow compositing changed %d opaque beauty pixels' % changed)
    return result


def check_manifest(path: Path) -> list[str]:
    try:
        manifest = json.loads(path.read_text(encoding='utf-8-sig'))
    except (OSError, ValueError) as error:
        return ['cannot read manifest: %s' % error]
    errors = []
    render = manifest.get('render') or {}
    for key, value in spec.RENDER.items():
        actual = render.get(key)
        same = abs(actual-value) < 1e-4 if isinstance(value, (int, float)) and not isinstance(value, bool) \
            and isinstance(actual, (int, float)) else actual == value
        if not same:
            errors.append('render.%s must be %r, got %r' % (key, value, actual))
    if render.get('seed') != spec.SEED or render.get('adaptive_sampling', False):
        errors.append('render must use seed %d without adaptive sampling' % spec.SEED)
    for name, value in (manifest.get('palette') or {}).items():
        if not isinstance(value, str) or value.upper() not in PALETTE_HEX:
            errors.append('palette %s=%r is not a palette colour' % (name, value))
    catalog = spec.by_id()
    for asset in manifest.get('assets') or []:
        asset_id = asset.get('id')
        entry = catalog.get(asset_id)
        if entry is None:
            errors.append('%s: not in the sprite catalog' % asset_id)
            continue
        if asset.get('file') != spec.sprite_path(entry):
            errors.append('%s: file must be %s' % (asset_id, spec.sprite_path(entry)))
        if list(asset.get('canvas') or []) != list(entry['canvas']):
            errors.append('%s: canvas must be %s' % (asset_id, list(entry['canvas'])))
        max_yaw = spec.KIND_RULES[entry['kind']].get('max_yaw')
        yaw = (asset.get('camera') or {}).get('yaw_deg')
        if max_yaw is not None and (yaw is None or abs(yaw) > max_yaw):
            errors.append('%s: camera |yaw| must be <= %s, got %r' % (asset_id, max_yaw, yaw))
        for material in asset.get('materials') or []:
            hex_value = material.get('palette_srgb')
            if not isinstance(hex_value, str) or hex_value.upper() not in PALETTE_HEX \
                    or spec.PALETTE.get(material.get('palette_name')) != hex_value:
                errors.append('%s: material %s has no palette colour (%r)' % (asset_id, material.get('name'), hex_value))
            for key, value in spec.MATERIAL.items():
                actual = material.get(key)
                if not isinstance(actual, (int, float)) or abs(actual-value) > 1e-4:
                    errors.append('%s: material %s needs a Principled BSDF with %s %s, got %r'
                                  % (asset_id, material.get('name'), key, value, actual))
    return errors


def manifest_category(path: Path):
    try:
        data = json.loads(path.read_text(encoding='utf-8-sig'))
    except (OSError, ValueError):
        return None
    return data.get('category') if isinstance(data, dict) else None


def validate(root: Path = ROOT, complete: bool = False, owners=None) -> dict:
    root = Path(root)
    catalog = [entry for entry in spec.CATALOG if owners is None or entry['owner'] in owners]
    sprites = [check_sprite(entry, root) for entry in catalog]
    manifest_paths = sorted((root/MANIFESTS).glob('*.manifest.json'))
    if owners is not None:
        manifest_paths = [path for path in manifest_paths if manifest_category(path) in owners]
    manifests = [{'file': path.relative_to(root).as_posix(), 'errors': check_manifest(path)}
                 for path in manifest_paths]
    known = {spec.sprite_path(entry) for entry in spec.CATALOG}
    if owners is None:
        scan_dirs = [root/SPRITES] if (root/SPRITES).is_dir() else []
    else:
        owned_folders = sorted({entry['folder'] for entry in spec.CATALOG if entry['owner'] in owners})
        scan_dirs = [root/SPRITES/folder for folder in owned_folders if (root/SPRITES/folder).is_dir()]
    stray = sorted(path.relative_to(root).as_posix() for scan_dir in scan_dirs for path in scan_dir.rglob('*.png'))
    stray = [path for path in stray if path not in known]
    errors = ['%s: %s' % (s['id'], message) for s in sprites for message in s['errors']]
    errors += ['%s: %s' % (m['file'], message) for m in manifests for message in m['errors']]
    errors += ['%s: PNG is not in the sprite catalog' % path for path in stray]
    missing = [s['id'] for s in sprites if not s['exists']]
    if complete:
        errors += ['%s: PNG is missing' % sprite_id for sprite_id in missing]
    for sprite in sprites:
        sprite['passed'] = sprite['exists'] and not sprite['errors']
    return {
        'source_spec': 'docs/superpowers/specs/2026-09-26-lowpoly-art-concept-design.md',
        'complete_required': complete,
        'owners': sorted(owners) if owners is not None else None,
        'passed': not errors,
        'total': len(catalog),
        'present': sum(s['exists'] for s in sprites),
        'missing': missing,
        'errors': errors,
        'warnings': ['%s: %s' % (s['id'], message) for s in sprites for message in s['warnings']],
        'manifests': manifests,
        'stray_pngs': stray,
        'sprites': sprites,
    }


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--complete', action='store_true', help='fail for every missing catalog PNG')
    parser.add_argument('--owner', action='append', default=[], metavar='NAME',
                        help='only check this catalog owner (repeatable, or comma-separated); '
                             'owners: %s' % ', '.join(OWNERS))
    parser.add_argument('--report', type=Path, default=ART/'ui-sprites-validation.json',
                        help='JSON report path (default: Art/Blender/ui-sprites-validation.json)')
    parser.add_argument('--root', type=Path, default=ROOT, help=argparse.SUPPRESS)
    args = parser.parse_args(argv)
    requested = [name for group in args.owner for name in group.split(',') if name]
    owners = sorted(set(requested)) if requested else None
    if owners is not None:
        unknown = sorted(set(owners) - set(OWNERS))
        if unknown:
            parser.error('unknown --owner %s; choose from %s' % (', '.join(unknown), ', '.join(OWNERS)))
    report = validate(args.root, args.complete, owners)
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    for message in report['warnings']:
        print('NOTE:', message)
    suffix = ' owner=%s' % ','.join(owners) if owners is not None else ''
    if report['passed']:
        print('UI_SPRITES_VALID %d/%d%s' % (report['present'], report['total'], suffix))
    else:
        print('UI_SPRITES_INVALID %d/%d present, %d failures%s'
              % (report['present'], report['total'], len(report['errors']), suffix))
        for message in report['errors']:
            print('FAIL:', message)
    print('Report:', args.report)
    return 0 if report['passed'] else 1


if __name__ == '__main__':
    sys.exit(main())
