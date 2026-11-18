#!/usr/bin/env python3
"""Convert reviewed Production72 cells to separate native pixel props; no API calls."""
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw

from convert_sources import components, export, keyed

ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / 'ArtSource/Higgsfield/PromptPacks/Production72'


def main():
    manifest = json.loads((PACK / 'manifest.json').read_text())
    survival = [
        ('PROP_UtilityBills_01', 20),
        ('PROP_TaxEnvelopes_01', 20),
        ('PROP_InstantNoodles_01', 12),
        ('PROP_TakeawayCoffee_01', 6),
        ('PROP_EnergyDrink_01', 5),
        ('INT_BudgetDeskChair_01', 28),
        ('PROP_OvertimeMeal_01', 18),
        ('PROP_BrokenUmbrella_01', 32),
        ('PROP_MovingBoxes_01', 40),
        ('PROP_JobApplication_01', 18),
        ('PROP_LoanFolder_01', 18),
        ('PROP_ExamBooks_01', 20),
    ]
    definitions = {
        71: (4, 3, {index: spec for index, spec in enumerate(survival)}),
        67: (4, 4, {
            5: ('ENV_City_Recycling_01', 66),
            10: ('ENV_City_RoadBarrier_01', 76),
            11: ('ENV_City_Hydrant_01', 32),
        }),
    }
    converted = []
    for number, (columns, rows, specs) in definitions.items():
        entry = manifest['prompts'][number - 1]
        selection = entry.get('selected', {})
        if selection.get('status') != 'APPROVED_REFERENCE':
            raise ValueError(f'Review and select source {number} before conversion')
        source = ROOT / selection['path']
        if hashlib.sha256(source.read_bytes()).hexdigest() != selection['sha256']:
            raise ValueError('Selected source changed after review')
        image = keyed(source)
        source_size = Image.open(source).size
        isolated = components(image) if number == 67 else []
        if number == 67 and len(isolated) != 14:
            raise ValueError('Reviewed urban sheet must contain fourteen isolated silhouettes')
        selection['unityAssets'] = []
        for cell, (name, width) in specs.items():
            column, row = cell % columns, cell // columns
            cell_box = (
                round(column * image.width / columns),
                round(row * image.height / rows),
                round((column + 1) * image.width / columns),
                round((row + 1) * image.height / rows),
            )
            local = image.crop(cell_box).getchannel('A').getbbox()
            if local is None:
                raise ValueError(f'Empty cell {number}/{cell}')
            box = (cell_box[0] + local[0], cell_box[1] + local[1],
                   cell_box[0] + local[2], cell_box[1] + local[3])
            if number == 67:
                # The model's rows are uneven. Fixed cells cut adjacent tall props.
                center_x = (column + .5) / columns
                center_y = (row + .5) / rows
                distance = lambda item: (
                    ((item[1][0] + item[1][2]) / (2 * image.width) - center_x) ** 2
                    + ((item[1][1] + item[1][3]) / (2 * image.height) - center_y) ** 2
                )
                matched = min(isolated, key=distance)
                if distance(matched) > .12 ** 2:
                    raise ValueError('Silhouette is outside reviewed cell tolerance')
                box = matched[1]
                isolated.remove(matched)
            result = export(image, box, name, width)
            result.update(
                sourcePrompt=number, candidate=selection['candidate'],
                sourcePath=selection['path'], sourceSha256=selection['sha256'],
                sourceSize=list(source_size), analysisSize=list(image.size),
                cell=cell, grid=[columns, rows],
                prefabPath=f'Assets/_Game/Prefabs/World/LivingCity/{name}.prefab',
                qa='CLEANUP_COMPLETE; visual edge/text review and Unity import verification required',
            )
            converted.append(result)
            selection['unityAssets'].append(result['unityPath'])
    (PACK / 'conversion.json').write_text(json.dumps(converted, indent=2) + '\n')
    (PACK / 'manifest.json').write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + '\n'
    )
    board = Image.new('RGB', (1200, 300 * ((len(converted) + 3) // 4)), (46, 46, 56))
    labels = ImageDraw.Draw(board)
    checks = []
    for index, result in enumerate(converted):
        path = ROOT / result['unityPath']
        image = Image.open(path).convert('RGBA')
        pixels = image.get_flattened_data()
        alpha = {pixel[3] for pixel in pixels}
        colors = {pixel[:3] for pixel in pixels if pixel[3]}
        if not alpha <= {0, 255} or len(colors) > 96:
            raise ValueError('Pixel alpha/palette QA failed: ' + result['name'])
        if image.getchannel('A').getbbox() != (2, 2, image.width - 2, image.height - 2):
            raise ValueError('Pixel crop margin failed: ' + result['name'])
        checks.append({'name': result['name'], 'size': list(image.size),
                       'opaqueColors': len(colors), 'alpha': sorted(alpha), 'passed': True})
        image = image.resize((image.width * 3, image.height * 3), Image.Resampling.NEAREST)
        x, y = index % 4 * 300, index // 4 * 300
        board.paste(image, (x + (300 - image.width) // 2, y + 40), image)
        labels.text((x + 8, y + 10), result['name'], fill='white')
    board.save(ROOT / 'ArtSource/Higgsfield/Selected/Production72/PROPS_CLEANUP_BOARD.png')
    (PACK / 'pixel_audit.json').write_text(
        json.dumps({'checkedProps': len(checks), 'passed': True, 'checks': checks}, indent=2) + '\n'
    )
    print(f'Converted {len(converted)} separate props; review before Unity import')


if __name__ == '__main__':
    main()
