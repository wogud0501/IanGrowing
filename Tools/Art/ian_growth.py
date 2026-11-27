"""Extract the approved growth sheet and build front-view sprite motion. Requires Pillow."""
from pathlib import Path
from math import cos, sin, pi
from itertools import groupby
import hashlib
import json

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'Assets/_Game/Art/Characters/Ian'
SOURCE = ART / 'Reference/Ian_OfficialGrowth.png'
DEST = ART / 'Growth/Official'
# Separators are in transparent gutters. Equal-height slicing clips the lower rows' hair.
ROWS = (0, 276, 544, 814, 1122)
COLS = (0, 280, 561, 841, 1122, 1402)
AGES = (0, 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 22, 26, 30, 35, 40, 45, 50, 55, 65)
MARGIN = 5


# First pixel row beneath the chin outline of each exposed school-age neckline,
# measured in the lossless crop. The infant collar and headphone silhouettes are left alone.
COLLARS = {"S03": 112, "S04": 112, "S05": 120, "S06": 118, "S07": 113, "S08": 119}


def collar_presentation(front, stage):
    top = COLLARS.get(stage)
    if top is None:
        return front
    result = front.copy()
    cx = front.width // 2
    # Resample only this 43-pixel patch. Pin the jaw, outer shoulders and lower shirt.
    # Two extra side pixels and two rows of neckline survive the small world camera.
    for dy in range(1, 15):
        weight = min(1, dy / 2) * min(1, (15 - dy) / 6)
        for dx in range(-21, 22):
            edge = max(0, 1 - abs(dx) / 21)
            sx = round(dx / (1 + .55 * edge * weight))
            lift = min(dy * .38, 2) * weight * min(1, edge * 2)
            sy = top + round(dy - lift)
            result.putpixel((cx + dx, top + dy), front.getpixel((cx + sx, sy)))
    return result


def stride(original, phase, stride_fraction, direction):
    """Articulate the planted and swinging legs. Sample the far leg first in profile."""
    width, height = original.size
    bottom = height - MARGIN - 1
    profile = direction == 2
    diagonal = direction > 2
    joint = round(height * .77)
    pose = original.copy()
    pose.paste((0, 0, 0, 0), (0, joint, width, height))
    # The source has hands in pockets. Rotate the shoulder line slightly against the stepping leg.
    chest = round(height * .55)
    for y in range(chest, joint):
        weight = sin(pi * (y - chest) / (joint - chest))
        offset = round(cos(phase) * height * .009 * weight)
        if offset:
            pose.paste((0, 0, 0, 0), (0, y, width, y + 1))
            pose.paste(original.crop((0, y, width, y + 1)), (offset, y))
    for side in (-1, 1):
        x0, x1 = (0, width) if profile else ((0, width // 2) if side < 0 else (width // 2, width))
        leg = original.crop((x0, joint, x1, height))
        visible = leg.getchannel('A').point(lambda a: 255 if a >= 64 else 0).getbbox()
        sole = joint + visible[3] - 1
        progress = (phase / (2*pi) + (.5 if side > 0 else 0)) % 1
        # Half a cycle of linear support opposes world translation. Lift only the returning foot.
        swing = 1 - abs(4 * progress - 2)
        lift = max(0, -sin(progress * 2*pi))
        landing = bottom - round(lift * height * .046)
        for y in range(joint, landing + 1):
            bend = (y - joint) / (landing - joint)
            old_y = joint + round(bend * (sole - joint))
            reach = stride_fraction * .25 if profile else stride_fraction * .16 if diagonal else .019
            shift = round(swing * bend * height * reach)
            pose.alpha_composite(original.crop((x0, old_y, x1, old_y + 1)), (x0 + shift, y))
    return pose


def alpha_box(image):
    return image.getchannel('A').point(lambda a: 255 if a >= 64 else 0).getbbox()


def views(path, columns, rows):
    image = Image.open(path).convert('RGBA')
    mask = image.getchannel('A').point(lambda a: 255 if a >= 64 else 0)
    bands = []
    y = 0
    for occupied, values in groupby(mask.getprojection()[1]):
        length = len(list(values))
        if occupied and length > 12:
            bands.append((y, y + length))
        y += length
    if len(bands) != rows:
        raise ValueError(f'{path} has {len(bands)} rows')
    result = []
    for top, bottom in bands:
        for col in range(columns):
            tile = image.crop((round(col * image.width / columns), top,
                               round((col + 1) * image.width / columns), bottom))
            result.append(tile.crop(alpha_box(tile)))
    return result


def fit_height(image, pixels):
    return image.resize((round(image.width * pixels / image.height), pixels), Image.Resampling.LANCZOS)


def centered(image, size):
    tile = Image.new('RGBA', size)
    tile.paste(image, ((tile.width - image.width) // 2, tile.height - MARGIN - image.height))
    return tile


def bend(original, amount, seated):
    """Hinge at the hips. Keep the head, eyes and clothing together on the same layer."""
    w, h = original.size
    hip = round(h * (.81 if seated else .77))
    pose = original.copy()
    pose.paste((0, 0, 0, 0), (0, 0, w, hip - 2))
    top = original.copy()
    top.paste((0, 0, 0, 0), (0, hip, w, h))
    pose.alpha_composite(top.rotate(amount, Image.Resampling.BICUBIC, center=(w / 2, hip)))
    return pose


def bake():
    with Image.open(SOURCE) as image:
        if image.mode != 'RGBA' or image.size != (COLS[-1], ROWS[-1]):
            raise ValueError('Unrecognized official growth sheet')
        DEST.mkdir(parents=True, exist_ok=True)
        regions = []
        source_art = ROOT / 'ArtSource/Ian/Growth'
        side, back, sitting, sleeping, reclining, frontleft, backleft = [views(source_art / (name + '.png'), 5, 4)
                                        for name in ('Left', 'Back', 'Seated', 'Sleep', 'Recline', 'FrontLeft', 'BackLeft')]
        toddler = views(source_art / 'ToddlerFirstSteps.png', 3, 1)
        mask = image.getchannel('A').point(lambda a: 255 if a >= 64 else 0)
        for i, age in enumerate(AGES):
            row, col = divmod(i, 5)
            cell = (COLS[col], ROWS[row], COLS[col + 1], ROWS[row + 1])
            x0, y0, x1, y1 = mask.crop(cell).getbbox()
            rect = (cell[0] + x0 - MARGIN, cell[1] + y0 - MARGIN,
                    cell[0] + x1 + MARGIN, cell[1] + y1 + MARGIN)
            assert cell[0] <= rect[0] < rect[2] <= cell[2] and cell[1] <= rect[1] < rect[3] <= cell[3]
            stage = f'S{i+1:02d}'
            front = image.crop(rect)
            front.save(DEST / f'Ian_{stage}.png')
            assert Image.open(DEST / f'Ian_{stage}.png').tobytes() == image.crop(rect).tobytes()
            if stage in COLLARS:
                front = collar_presentation(front, stage)
                presentation = DEST.parent / 'Presentation'
                presentation.mkdir(exist_ok=True)
                front.save(presentation / f'Ian_{stage}.png')
            w, h = front.size
            body = front.crop((MARGIN, MARGIN, w-MARGIN, h-MARGIN))
            directions = [body, back[i], side[i], frontleft[i], backleft[i]]
            moving = [toddler[0], toddler[2], toddler[1], frontleft[i], backleft[i]] if i == 1 else directions
            size = h - 2*MARGIN
            idle = [fit_height(view, size) for view in directions]
            steps = [fit_height(view, size) for view in moving]
            seat = fit_height(sitting[i], round(size * (1 if i < 2 else .84)))
            sleep = fit_height(sleeping[i], round(size * .94))
            recline = fit_height(reclining[i], round(size * .87))
            tile_w = max(v.width for v in idle + steps + [seat, sleep, recline]) + 2*(MARGIN+round(.13*h))
            tile = (tile_w, h)
            atlas_width = (tile_w * 7 + 3) // 4 * 4
            if max(atlas_width, h * 6) > 2048:
                raise ValueError(f'{stage} exceeds the full-resolution mobile import limit')
            atlas = Image.new('RGBA', (atlas_width, h*6))
            def put(frame, col, row):
                atlas.paste(frame, (col*tile_w, row*h))
            stride_fraction = .32 if age < 3 else .42 if age < 13 else .52
            for direction, atlas_row in enumerate((0, 1, 2, 4, 5)):
                put(centered(idle[direction], tile), 0, atlas_row)
                if i > 0:
                    moving_pose = centered(steps[direction], tile)
                    for frame in range(6):
                        put(stride(moving_pose, frame*pi/3, stride_fraction, direction), frame+1, atlas_row)
            neutral = centered(body, tile)
            for idx, angle in enumerate((2, 4, 6)):
                put(bend(neutral, angle, i < 2), idx, 3)
            put(centered(seat, tile), 3, 3)
            put(centered(sleep, tile), 4, 3)
            put(centered(recline, tile), 5, 3)
            atlas.save(DEST / f'Ian_{stage}_Motion.png')
            # For the first two seated figures use the central, seat-bearing
            # contour for ground rather than either raised toe.
            central = mask.crop(rect).crop((int(w*.42), 0, int(w*.58), h)).getbbox()
            contact = h - central[3] if i < 2 else MARGIN
            regions.append(dict(id=stage, minAge=age, x=rect[0], y=rect[1], width=w,
                                height=h, tileWidth=tile_w, contact=contact, seated=i < 2))
        manifest = dict(sha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(), stages=regions)
        (DEST / 'GrowthStages.json').write_text(json.dumps(manifest, indent=2) + '\n')


if __name__ == '__main__':
    bake()
