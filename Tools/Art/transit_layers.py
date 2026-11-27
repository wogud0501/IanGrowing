"""Slice the approved cabin plates and the actual doors from the original vehicle sprites."""
import json
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / 'Assets/_Game/Art/Transit/Resources/CityTransit'
SOURCE = ROOT / 'ArtSource/Higgsfield/Selected/Transit/Cabins.png'


def exterior(image, name, doors):
    shell = image.copy()
    for door, polygon in doors.items():
        mask = Image.new('L', image.size)
        ImageDraw.Draw(mask).polygon(polygon, fill=255)
        box = mask.getbbox()
        leaf = image.copy()
        leaf.putalpha(mask)
        leaf.crop(box).save(DEST / f'{name}{door}.png')
        shell.paste((0, 0, 0, 0), box, mask.crop(box))
    shell.save(DEST / (name + 'Shell.png'))


# Pixel coordinates on the 512 x 170 cabin plates. Each polygon covers exposed glass,
# leaving the original grab rails, window gaskets, frame and handles in the foreground.
GLASS = {
    'Rail': (170, [
        [(107,54),(131,54),(131,97),(108,97)], [(139,54),(185,54),(185,97),(139,97)],
        [(195,54),(244,54),(244,96),(195,97)], [(270,54),(292,54),(292,97),(270,97)],
        [(298,54),(366,54),(366,97),(298,97)], [(381,54),(405,54),(405,97),(381,97)]]),
    'Bus': (154, [
        [(155,44),(230,44),(230,78),(155,78)], [(255,44),(309,44),(309,77),(255,77)],
        [(319,44),(392,44),(392,77),(319,77)]]),
    'Cab': (140, [
        [(162,26),(327,24),(320,73),(156,76)], [(337,27),(350,37),(363,64),(359,72),(328,72)]]),
}


def glass(plate, name):
    transmission = plate.getchannel('A')
    opacity, panes = GLASS[name]
    draw = ImageDraw.Draw(transmission)
    for pane in panes:
        draw.polygon(pane, fill=opacity)
    plate.putalpha(transmission)


def passenger_grip():
    # Articulate the existing passenger's right sleeve and hand about the shoulder and elbow.
    # The completed fist at (87.5, 70.5) is the registration point on the carriage rail.
    original = Image.open(DEST / 'Passenger0.png').convert('RGBA')
    canvas = Image.new('RGBA', (112, original.height))
    canvas.paste(original)
    arm = Image.new('L', canvas.size)
    ImageDraw.Draw(arm).polygon(
        [(63, 72), (69, 76), (76, 92), (82, 101), (82, 120), (75, 125),
         (66, 125), (66, 110), (64, 99), (63, 92), (62, 82)], fill=255)
    limb = Image.new('RGBA', canvas.size)
    limb.paste(canvas, mask=arm)
    # Close the jacket and trouser outline that was hidden by the lowered arm.
    edge = [(63, 73), (62, 80), (63, 91), (65, 101), (67, 109),
            (66, 113), (66, 122), (68, 127)]
    outline = ImageDraw.Draw(canvas)
    outline.polygon(edge + [(111, 127), (111, 73)], fill=(0, 0, 0, 0))
    outline.line(edge, fill=original.getpixel((64, 94)), width=1)
    upper = limb.copy()
    upper.paste((0, 0, 0, 0), (0, 96, *canvas.size))
    lower = limb.copy()
    lower.paste((0, 0, 0, 0), (0, 0, canvas.width, 91))
    canvas.alpha_composite(upper.rotate(25, center=(63, 74), resample=Image.Resampling.BICUBIC))
    lower = lower.rotate(136, center=(68, 93), translate=(8, -4), resample=Image.Resampling.BICUBIC)
    canvas.alpha_composite(lower)
    canvas.save(DEST / 'PassengerGrip.png')


def cab_hand_contacts():
    # Locate the visible right hand on each official standing pose. The cutscene
    # articulates the original pixels directly in its mesh, without another texture atlas.
    growth = ROOT / 'Assets/_Game/Art/Characters/Ian/Growth'
    stages = []
    for stage in json.loads((growth / 'Official/GrowthStages.json').read_text())['stages'][2:]:
        filename = 'Ian_' + stage['id'] + '.png'
        path = growth / 'Presentation' / filename
        with Image.open(path if path.exists() else growth / 'Official' / filename) as image:
            image = image.convert('RGBA')
            w, h = image.size
            bands = []
            for y in range(round(h * .565), round(h * .83)):
                row = []
                for x in range(round(w * .55), round(w * .91)):
                    r, g, b, a = image.getpixel((x, y))
                    if a > 190 and min(r, g, b) > 133 and max(r, g, b) - min(r, g, b) < 77:
                        row.append(x)
                if len(row) >= 2:
                    if not bands or y != bands[-1][-1][0] + 1:
                        bands.append([])
                    bands[-1].append((y, row))
            band = max(bands, key=len)
            xs = [x for _, row in band for x in row]
            top, bottom = band[0][0], band[-1][0] + 1
            inside, outside = min(xs), max(xs) + 1
            stages.append(dict(sprite=filename[:-4], cuffY=top, handBottom=bottom,
                               insideX=inside, outsideX=outside))
    (DEST / 'CabHandContacts.json').write_text(json.dumps(dict(stages=stages), indent=2) + '\n')


def main():
    sheet = Image.open(SOURCE).convert('RGBA')
    assert sheet.size == (1024, 1024)
    for name, area in [('Rail', (0, 1, 1024, 339)), ('Bus', (0, 345, 1024, 678)),
                       ('Cab', (0, 685, 1024, 1024))]:
        plate = sheet.crop(area).resize((512, 170), Image.Resampling.LANCZOS)
        glass(plate, name)
        plate.save(DEST / (name + 'Cabin.png'))
    # A repeatable view through the passenger window; the trim and pillars remain fixed.
    sheet.crop((332, 748, 626, 813)).resize((224, 55), Image.Resampling.LANCZOS).save(DEST / 'CityWindow.png')
    bus = Image.open(ROOT / 'Assets/_Game/Art/WorldLife/VEH_Bus_01.png').convert('RGBA').crop((2,2,194,86))
    exterior(bus, 'Bus', {'Front': [(154,32),(172,32),(172,78),(154,78)],
                           'Rear': [(74,32),(99,32),(99,78),(74,78)]})
    taxi = Image.open(DEST / 'Taxi.png').convert('RGBA')
    exterior(taxi, 'Cab', {'Leaf': [(59,39),(73,26),(87,18),(109,17),(112,84),(82,84),
                                     (75,78),(72,68),(66,56)]})
    passenger_grip()
    cab_hand_contacts()


if __name__ == '__main__':
    main()
