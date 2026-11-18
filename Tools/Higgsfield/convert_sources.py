#!/usr/bin/env python3
"""Convert explicitly selected isolated source sheets to individual pixel sprites.
Requires Pillow. Raw scene concepts are never imported as runtime backgrounds.
"""
import json
from collections import deque
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'ArtSource/Higgsfield'
OUTPUT = ROOT / 'Assets/_Game/Art/WorldLife'


def keyed(source):
    image = Image.open(source).convert('RGBA')
    image.thumbnail((1100, 900), Image.Resampling.NEAREST)
    pixels = image.load()
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, a = pixels[x, y]
            if r > 140 and b > 140 and g < 125 and r > g * 1.65 and b > g * 1.65:
                pixels[x, y] = (0, 0, 0, 0)
            else:
                pixels[x, y] = (r, g, b, 255)
    return image


def components(image):
    mask = image.getchannel('A')
    pixels = mask.load()
    seen = set()
    found = []
    for y in range(image.height):
        for x in range(image.width):
            if pixels[x, y] == 0 or (x, y) in seen:
                continue
            queue = deque([(x, y)])
            seen.add((x, y))
            count = 0
            left = right = x
            top = bottom = y
            while queue:
                px, py = queue.popleft()
                count += 1
                left, right = min(left, px), max(right, px)
                top, bottom = min(top, py), max(bottom, py)
                for nx, ny in [(px - 1, py), (px + 1, py), (px, py - 1), (px, py + 1)]:
                    if 0 <= nx < image.width and 0 <= ny < image.height and pixels[nx, ny] and (nx, ny) not in seen:
                        seen.add((nx, ny))
                        queue.append((nx, ny))
            if count >= 100:
                found.append((count, (left, top, right + 1, bottom + 1)))
    return found


def export(image, box, name, width, height=None, blank=None):
    crop = image.crop(box)
    if height is None:
        height = round(crop.height * width / crop.width)
    else:
        width = round(crop.width * height / crop.height)
    crop = crop.resize((width, height), Image.Resampling.NEAREST)
    alpha = crop.getchannel('A')
    crop = crop.convert('RGB').quantize(colors=96, dither=Image.Dither.NONE).convert('RGBA')
    crop.putalpha(alpha.point(lambda a: 255 if a >= 128 else 0))
    if blank:
        draw = ImageDraw.Draw(crop)
        for region, color in blank:
            draw.rectangle(region, fill=color)
    result = Image.new('RGBA', (width + 4, height + 4))
    result.paste(crop, (2, 2))
    path = OUTPUT / (name + '.png')
    result.save(path)
    return {'name': name, 'sourceBox': list(box), 'size': list(result.size),
            'unityPath': str(path.relative_to(ROOT)), 'ppu': 40, 'alpha': 'binary', 'paletteLimit': 96}


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    selected = json.loads((SOURCE / 'selection.json').read_text())
    metadata = []
    definitions = {
        'urban_props': [
            ('ENV_City_Lamp_01', 42, 128), ('ENV_City_Signal_01', 30, 104),
            ('ENV_City_Bench_01', 104, None), ('ENV_City_Bin_01', 32, None),
            ('ENV_City_UtilityPole_01', 30, 140), ('ENV_City_UtilityBox_01', 65, None),
            ('ENV_City_BicycleRack_01', 92, None), ('ENV_City_BusStop_01', 160, None),
            ('ENV_City_Planter_01', 72, None), ('ENV_City_Vending_01', 48, None),
            ('ENV_City_AC_01', 48, None), ('ENV_City_Parcels_01', 52, None)],
        'vehicles': [('VEH_Sedan_01', 116, None), ('VEH_Taxi_01', 116, None),
                     ('VEH_Bus_01', 192, None), ('VEH_DeliveryVan_01', 124, None),
                     ('VEH_DeliveryScooter_01', 62, None), ('VEH_Bicycle_01', 66, None)],
        'store_props': [('INT_Store_ColdShelf_01', 76, None), ('INT_Store_SnackShelf_01', 90, None),
                        ('INT_Store_Counter_01', 128, None), ('INT_Store_Microwave_01', 44, None),
                        ('INT_Store_NoodleRack_01', 60, None), ('INT_Store_FoodBasket_01', 42, None)]}
    for key, specs in definitions.items():
        if key not in selected:
            continue
        if selected[key]['status'] != 'APPROVED_REFERENCE':
            raise ValueError('Unapproved source ' + key)
        image = keyed(SOURCE / 'Selected' / (key + '.png'))
        found = sorted(components(image), reverse=True)[:len(specs)]
        rows = 3 if key == 'urban_props' else 2
        ordered = []
        for row in range(rows):
            local = [box for _, box in found if int(((box[1] + box[3]) * .5) * rows / image.height) == row]
            ordered += sorted(local, key=lambda box: box[0])
        if len(ordered) != len(specs):
            raise ValueError('Sheet segmentation needs manual review: ' + key)
        for spec, box in zip(specs, ordered):
            name, width, height = spec
            result = export(image, box, name, width, height)
            result['source'] = key
            result['candidate'] = selected[key]['candidate']
            metadata.append(result)
    for key, name, width in [('apartment_module', 'BLD_Apartment_01', 168),
                             ('convenience_module', 'BLD_ConvenienceStore_01', 208)]:
        if key not in selected or selected[key]['status'] != 'APPROVED_REFERENCE':
            continue
        image = keyed(SOURCE / 'Selected' / (key + '.png'))
        result = export(image, image.getchannel('A').getbbox(), name, width)
        result.update(source=key, candidate=selected[key]['candidate'])
        metadata.append(result)
        mask = Image.open(OUTPUT / (name + '.png')).convert('RGBA')
        pixels = mask.load()
        for y in range(mask.height):
            for x in range(mask.width):
                r, g, b, a = pixels[x, y]
                warm = r >= 145 and g >= 115 and b < g * .8 and r >= g * 1.05
                teal = g > 140 and b > 125 and r < g * .85
                if not (warm or teal):
                    pixels[x, y] = (0, 0, 0, 0)
        mask_name = name.replace('_01', '_Lights_01')
        mask.save(OUTPUT / (mask_name + '.png'))
        metadata.append({'name': mask_name, 'source': key, 'candidate': selected[key]['candidate'],
                         'unityPath': str((OUTPUT / (mask_name + '.png')).relative_to(ROOT)),
                         'derivedFrom': name, 'operation': 'warm/teal pixel emissive mask', 'ppu': 40})
    (SOURCE / 'conversion.json').write_text(json.dumps(metadata, indent=2) + '\n')
    print('Converted isolated sources:', len(metadata), '; manual edge/text QA required before import')


if __name__ == '__main__':
    main()
