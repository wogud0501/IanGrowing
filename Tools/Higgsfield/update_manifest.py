#!/usr/bin/env python3
"""Reconcile stored Higgsfield results and reviewed selections; never generates assets."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'ArtSource/Higgsfield'


def main():
    selections = json.loads((SOURCE / 'selection.json').read_text())
    converted = json.loads((SOURCE / 'conversion.json').read_text())
    rejected = {
        'convenience_interior_a': 'Rejected: exterior city and inset instead of standalone interior',
        'convenience_interior_b': 'Rejected: exterior city and inset instead of standalone interior',
        'npc_archetypes_a': 'Rejected: city panorama consumes sheet area',
        'npc_archetypes_b': 'Rejected: city panorama consumes sheet area',
    }
    records = []
    for path in sorted((SOURCE / 'Raw').glob('*.result.json')):
        try:
            jobs = json.loads(path.read_text())
        except ValueError:
            continue
        if isinstance(jobs, dict):
            jobs = [jobs]
        for job in jobs:
            candidate = path.name.replace('.result.json', '')
            refs = SOURCE / 'Prompts' / (candidate + '.refs.json')
            selected = next((key for key, selection in selections.items() if selection['candidate'] == candidate), None)
            records.append({
                'candidate': candidate, 'generationId': job.get('id'), 'createdAt': job.get('created_at'),
                'model': job.get('job_type'), 'status': job.get('status'), 'resultUrl': job.get('result_url'),
                'prompt': 'ArtSource/Higgsfield/Prompts/' + candidate + '.txt',
                'references': json.loads(refs.read_text()) if refs.exists() else [],
                'rawPath': 'ArtSource/Higgsfield/Raw/' + candidate + '.png',
                'qaStatus': 'APPROVED_REFERENCE' if selected else ('REJECTED' if candidate in rejected else 'NOT_SELECTED'),
                'qaNote': selections[selected]['qa'] if selected else rejected.get(candidate, 'Alternate candidate retained; no additional retry'),
            })
    for key, selection in selections.items():
        path = SOURCE / 'Selected' / (key + '.png')
        selection['selectedPath'] = str(path.relative_to(ROOT))
        selection['sha256'] = hashlib.sha256(path.read_bytes()).hexdigest()
        selection['reviewer'] = 'Codex visual QA; human production signoff pending'
        selection['unityAssets'] = [asset['unityPath'] for asset in converted if asset['source'] == key]
    manifest = {'phase': '3.3', 'generationCount': len(records), 'requestedCandidateCount': len(records) + 1, 'initialBatchUnreturnedResults': 1,
                'completed': sum(record['status'] == 'completed' for record in records),
                'approvedCount': len(selections), 'runtimeSpriteCount': len(converted),
                'requests': records, 'approved': selections, 'runtime': converted}
    (SOURCE / 'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n')
    lines = ['# Higgsfield asset manifest', '',
             f"2026-11-18 · {len(records)} candidates, {len(selections)} selected production references, {len(converted)} separated sprites/emissive masks (+ 2 authored rain VFX textures).", '',
             'Selections passed Codex visual QA; final human art approval remains open. RAW is outside Unity. Only separated, cleaned objects enter Assets.', '',
             '| Reference | Candidate | Unity outputs |', '|---|---|---|']
    for key, selection in selections.items():
        lines.append(f"| [{key}](../{selection['selectedPath']}) | {selection['candidate']} | {len(selection['unityAssets'])} |")
    lines += ['', '- Full prompts, references, timestamps, IDs, QA status, output URLs and SHA-256: [manifest.json](../ArtSource/Higgsfield/manifest.json).',
              '- Crop boxes, logical dimensions and alpha/palette rules: [conversion.json](../ArtSource/Higgsfield/conversion.json).',
              '- Four unsuitable initial sheet candidates were rejected. Each affected prompt received one focused revision and two replacements. No automatic generation retry.',
              '- Popcorn / Canvas / Moodboard endpoints were absent from the discovered catalog. Variants explicitly referenced the selected base image. The local master board contains all selected original references.',
              '- Initial CLI batch_size=2 requested two candidates but returned one result. Thus 65 candidates were requested, 64 completed result IDs were returned/downloaded, and one initial result ID remains unavailable. Later candidates used separately recorded single requests; no hidden result is claimed as verified.',
              '- Candidate rasters remain local outside Unity and Git. Selected source images, requests, results and download URLs are retained for reproduction.',
              '- No third-party game screenshots or real logos were used.']
    (ROOT / 'Docs/HIGGSFIELD_ASSET_MANIFEST.md').write_text('\n'.join(lines) + '\n')
    print('Candidates:', len(records), '; selected:', len(selections), '; converted:', len(converted))


if __name__ == '__main__':
    main()
