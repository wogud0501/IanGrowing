"""Register playable Ian using only the approved turnaround. Run with Pillow at the repository root."""
from collections import deque
from math import sin, pi
from pathlib import Path
from PIL import Image

ROOT = Path("Assets/_Game/Art/Characters/Ian")
SOURCE = ROOT / "Reference/Ian_OfficialTurnaround.png"
OUTPUT = ROOT / "Locomotion/IanOfficial.png"
CELL = 144
HEIGHT = 128
SOLE = 138
# Each rectangle excludes labels and neighbouring figures on the locked 1448 x 1086 sheet.
VIEWS = [(34,109,304,513), (580,598,861,1004), (864,111,1146,514), (1151,110,1427,513),
         (300,109,577,513), (579,110,855,513), (29,594,296,1005), (301,596,574,1005),
         (1147,631,1429,1004)]


def cut(sheet, rect):
    crop = sheet.crop(rect).convert("RGBA")
    # Flood only the connected white page. Enclosed whites in eyes and shoes survive.
    w, h = crop.size
    pixels = crop.load()
    frontier = deque([(0, 0)])
    while frontier:
        x, y = frontier.popleft()
        if not (0 <= x < w and 0 <= y < h):
            continue
        r, g, b, a = pixels[x, y]
        if not a or min(r, g, b) < 180:
            continue
        pixels[x, y] = (0, 0, 0, 0)
        frontier.extend(((x-1,y), (x+1,y), (x,y-1), (x,y+1)))
    crop = crop.crop(crop.getchannel("A").getbbox())
    return crop.resize((round(crop.width * HEIGHT / crop.height), HEIGHT), Image.Resampling.LANCZOS)


def limbs(figure, phase, profile=False, run=False):
    # The soles alternate support; the upper body stays exactly registered. The
    # side view has one visible trouser leg, so expose its far counterpart first.
    width, height = figure.size
    hip = round(height * .775)
    layer = Image.new("RGBA", (CELL, CELL))
    left = (CELL - width) // 2
    top = SOLE - height
    amplitude = (7 if run else 4) if profile else (3 if run else 2)
    for sign in (-1, 1):
        cycle = sin(phase) * sign
        x0, x1 = (0, width) if profile else ((0, width//2) if sign < 0 else (width//2, width))
        alpha = figure.crop((x0, hip, x1, height)).getchannel("A")
        sole = hip + alpha.point(lambda a: 255 if a >= 20 else 0).getbbox()[3] - 1
        # The turnaround has one shoe a pixel higher in the 3/4 view. Register
        # the supporting sole and swing only the free foot. Inverse sampling
        # keeps the lower trouser leg continuous when it straightens.
        ground = height - 1 - round(max(0, cycle) * (4 if run else 2))
        for y in range(hip-2, ground+1):
            bend = max(0, (y-hip)/(ground-hip))
            stride = round(cycle * amplitude * bend)
            sy = hip + round(bend * (sole-hip)) if y >= hip else y
            part = figure.crop((x0,sy,x1,sy+1))
            layer.alpha_composite(part, (left+x0+stride,top+y))
    layer.alpha_composite(figure.crop((0,0,width,hip+1)), (left,top))
    return layer


def still(figure):
    cell = Image.new("RGBA", (CELL,CELL))
    cell.alpha_composite(figure, ((CELL-figure.width)//2, SOLE-figure.height))
    return cell


def bake():
    sheet = Image.open(SOURCE)
    if sheet.size != (1448,1086):
        raise ValueError("Turnaround geometry changed; review the source view rectangles.")
    atlas = Image.new("RGBA", (16*CELL,len(VIEWS)*CELL))
    for row, rect in enumerate(VIEWS):
        figure = cut(sheet, rect)
        atlas.alpha_composite(still(figure), (0,row*CELL))
        for col in range(1,13):
            pose = limbs(figure, (col-1)%6*pi/3 + pi/6, row in (2,3), col>6)
            atlas.alpha_composite(pose, (col*CELL,row*CELL))
        # Rotate the complete upper body about the hip for a reach/settle. This
        # preserves the face and head proportions, including the headphones.
        base = still(figure)
        hip_y = SOLE-round(HEIGHT*.24)
        upper = base.copy()
        upper.paste((0,0,0,0), (0,hip_y,CELL,CELL))
        for col, degrees in enumerate((2,4,6), start=13):
            lower = base.copy()
            lower.paste((0,0,0,0), (0,0,CELL,hip_y-2))
            upper_pose = upper.rotate(degrees if row != 3 else -degrees,
                resample=Image.Resampling.BICUBIC, center=(CELL//2, hip_y))
            lower.alpha_composite(upper_pose)
            atlas.alpha_composite(lower, (col*CELL,row*CELL))
    atlas.save(OUTPUT, optimize=True)


if __name__ == "__main__":
    bake()
