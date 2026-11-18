#!/usr/bin/env python3
"""Prepare explicit Production72 candidates and record reviewed references. No API calls."""
import argparse
import hashlib
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'ArtSource/Higgsfield'
PACK = SOURCE / 'PromptPacks/Production72'
OFFICIAL_IAN = 'Assets/_Game/Art/Characters/Ian/Reference/Ian_OfficialDesign.png'


def read_manifest():
    return json.loads((PACK / 'manifest.json').read_text())


def write_manifest(manifest):
    (PACK / 'manifest.json').write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + '\n'
    )


def resolve_references(entry, manifest):
    dependency = entry.get('locationDependency')
    if dependency:
        selected = manifest['prompts'][dependency - 1].get('selected')
        if not selected:
            raise ValueError(f"Select location {dependency:03} before preparing {entry['id']:03}")
        references = [selected['path']]
    else:
        references = entry.get('seedReferences', []).copy()
    for reference in entry.get('styleReferences', []):
        if reference not in references:
            references.append(reference)
    if entry['includesIan']:
        references.append(OFFICIAL_IAN)
    for reference in references:
        if not (ROOT / reference).is_file():
            raise ValueError('Missing reference: ' + reference)
    return references


def prepare(ids):
    manifest = read_manifest()
    if len(ids) != len(set(ids)) or len(ids) > 12:
        raise ValueError('Use at most 12 distinct prompt IDs per explicit batch')
    prepared = []
    # Resolve the whole batch before writing any prompt files.
    entries = [(manifest['prompts'][number - 1], number) for number in ids]
    resolved = [(entry, resolve_references(entry, manifest)) for entry, _ in entries]
    for entry, references in resolved:
        master = (PACK / 'MASTER_STYLE.txt').read_text().strip()
        formula = (SOURCE / 'Prompts/STYLE_FORMULA.txt').read_text().strip()
        body = (PACK / entry['bodyFile']).read_text().strip()
        negative = (PACK / 'NEGATIVE_DIRECTION.txt').read_text().strip()
        additions = [entry.get('productionGuard', '')]
        if entry.get('locationDependency') or entry.get('preserveSeedGeometry'):
            additions.append(
                'The first attached image is the locked location. Preserve its camera, '
                'floor outline, door/window positions, walls, road widths and building '
                'footprints. Change only the requested props, people or lighting. '
                'Render one whole scene, never a collage, inset, map or split screen.'
            )
        if entry['includesIan']:
            additions.append(
                'The final attached image is the authoritative original Ian character. '
                'Keep his messy black hair, tired half-open eyes, small slim build, '
                'restrained face and dark desaturated clothing. Black headphones rest '
                'around his neck; black short sleeve top, black cargo pants, slippers. '
                'Adapt only to the explicitly requested age. Keep him at human game '
                'scale within the environment, never a large foreground portrait.'
            )
        prompt = '\n\n'.join(
            part for part in [master, body, formula, negative, *additions] if part
        ) + '\n'
        entry['resolvedReferences'] = references
        entry['referenceSha256'] = {
            reference: hashlib.sha256((ROOT / reference).read_bytes()).hexdigest()
            for reference in references
        }
        for candidate in entry['candidates']:
            name = candidate['name']
            prompt_path = SOURCE / 'Prompts' / (name + '.txt')
            refs_path = prompt_path.with_suffix('.refs.json')
            request = SOURCE / 'Raw' / (name + '.request.json')
            if request.exists():
                if prompt_path.read_text() != prompt:
                    raise ValueError('Submitted prompt is immutable: ' + name)
                if json.loads(refs_path.read_text()) != references:
                    raise ValueError('Submitted references are immutable: ' + name)
            else:
                prompt_path.write_text(prompt)
                refs_path.write_text(json.dumps(references, indent=2) + '\n')
            candidate['promptSha256'] = hashlib.sha256(prompt.encode()).hexdigest()
            prepared.append(name)
    write_manifest(manifest)
    print('Prepared (not submitted): ' + ' '.join(prepared))


def select(number, suffix, note):
    manifest = read_manifest()
    entry = manifest['prompts'][number - 1]
    candidate = next(c['name'] for c in entry['candidates'] if c['name'].endswith('_' + suffix))
    result_path = SOURCE / 'Raw' / (candidate + '.result.json')
    jobs = json.loads(result_path.read_text())
    if isinstance(jobs, dict):
        jobs = [jobs]
    if len(jobs) != 1 or jobs[0].get('status') != 'completed':
        raise ValueError('Selection requires one completed result')
    image = SOURCE / 'Raw' / (candidate + '.png')
    target = SOURCE / 'Selected/Production72' / f'{number:03}.png'
    target.parent.mkdir(parents=True, exist_ok=True)
    if entry.get('selected') and entry['selected']['candidate'] != candidate:
        # Submitted dependent variants must retain their immutable location reference.
        for dependent in manifest['prompts']:
            if dependent.get('locationDependency') == number:
                if any((SOURCE / 'Raw' / (c['name'] + '.request.json')).exists()
                       for c in dependent.get('previousCandidates', []) + dependent['candidates']):
                    raise ValueError('Location locked by already submitted variants')
    shutil.copyfile(image, target)
    entry['selected'] = {
        'candidate': candidate,
        'path': str(target.relative_to(ROOT)),
        'sha256': hashlib.sha256(target.read_bytes()).hexdigest(),
        'status': 'APPROVED_REFERENCE',
        'reviewer': 'Codex visual QA; human production signoff pending',
        'qa': note,
        'unityAssets': [],
    }
    write_manifest(manifest)
    print(f'Selected {number:03}: {candidate} (reference only)')


def report():
    manifest = read_manifest()
    requested = completed = selected = 0
    for entry in manifest['prompts']:
        selected += bool(entry.get('selected'))
        for candidate in entry.get('previousCandidates', []) + entry['candidates']:
            name = candidate['name']
            request_path = SOURCE / 'Raw' / (name + '.request.json')
            result_path = SOURCE / 'Raw' / (name + '.result.json')
            if not request_path.exists():
                candidate['status'] = 'NOT_SUBMITTED'
                continue
            requested += 1
            candidate['status'] = 'SUBMITTED'
            if not result_path.exists() or not result_path.stat().st_size:
                error_path = SOURCE / 'Raw' / (name + '.stderr.log')
                if error_path.exists() and error_path.stat().st_size:
                    candidate['status'] = 'NO_RESULT_ERROR'
                continue
            try:
                jobs = json.loads(result_path.read_text())
            except json.JSONDecodeError:
                # The CLI can still be writing its final JSON while a report is requested.
                candidate['status'] = 'PENDING_RESULT'
                continue
            if isinstance(jobs, dict):
                jobs = [jobs]
            candidate['results'] = [
                {key: job.get(key) for key in
                 ('id', 'created_at', 'job_type', 'status', 'result_url')}
                for job in jobs
            ]
            if len(jobs) == 1 and jobs[0].get('status') == 'completed':
                image = SOURCE / 'Raw' / (name + '.png')
                if image.is_file():
                    completed += 1
                    candidate['status'] = 'DOWNLOADED'
                    candidate['sha256'] = hashlib.sha256(image.read_bytes()).hexdigest()
                    candidate['rawPath'] = str(image.relative_to(ROOT))
    manifest['summary'] = {
        'registeredPrompts': len(manifest['prompts']),
        'attemptedCandidates': requested,
        'returnedResultIds': sum(len(c.get('results', [])) for e in manifest['prompts']
                                 for c in e.get('previousCandidates', []) + e['candidates']),
        'downloadedCandidates': completed,
        'selectedReferences': selected,
        'runtimeAssets': sum(len(e.get('selected', {}).get('unityAssets', []))
                             for e in manifest['prompts']),
    }
    write_manifest(manifest)
    print(json.dumps(manifest['summary'], ensure_ascii=False))


def audit():
    manifest = read_manifest()
    bodies = (PACK / 'BODIES.txt').read_text().strip().split('\n===\n')
    master = (PACK / 'MASTER_STYLE.txt').read_text().strip()
    negative = (PACK / 'NEGATIVE_DIRECTION.txt').read_text().strip()
    formula = (SOURCE / 'Prompts/STYLE_FORMULA.txt').read_text().strip()
    failures = []
    checks = 0

    def check(condition, message):
        nonlocal checks
        checks += 1
        if not condition:
            failures.append(message)

    check(len(manifest['prompts']) == len(bodies) == 72, 'Expected exactly 72 source prompts')
    for entry in manifest['prompts']:
        number = entry['id']
        body = (PACK / entry['bodyFile']).read_text()
        check(body.strip() == bodies[number - 1].strip(), f'Source body changed: {number}')
        check(hashlib.sha256(body.encode()).hexdigest() == entry['bodySha256'],
              f'Source hash mismatch: {number}')
        check(len(entry['candidates']) == 2, f'Expected exactly two candidates: {number}')
        for candidate in entry.get('previousCandidates', []) + entry['candidates']:
            name = candidate['name']
            prompt_path = SOURCE / 'Prompts' / (name + '.txt')
            if not prompt_path.exists():
                continue
            prompt = prompt_path.read_text()
            refs_path = prompt_path.with_suffix('.refs.json')
            refs = json.loads(refs_path.read_text())
            check(prompt.startswith(master), 'Master prefix missing: ' + name)
            check(body.strip() in prompt, 'Original body missing: ' + name)
            check(formula in prompt and negative in prompt, 'Style/negative missing: ' + name)
            check(hashlib.sha256(prompt.encode()).hexdigest() == candidate['promptSha256'],
                  'Submitted prompt changed: ' + name)
            check(not entry['includesIan'] or OFFICIAL_IAN in refs,
                  'Official Ian reference missing: ' + name)
            if entry.get('locationDependency'):
                parent = manifest['prompts'][entry['locationDependency'] - 1]
                check(refs[0] == parent['selected']['path'], 'Wrong locked location: ' + name)
            for reference in refs:
                check((ROOT / reference).is_file(), 'Missing reference: ' + reference)
                hashes = candidate.get('referenceSha256', entry.get('referenceSha256', {}))
                expected = hashes.get(reference)
                check(expected == hashlib.sha256((ROOT / reference).read_bytes()).hexdigest(),
                      'Reference changed after preparation: ' + reference)
        if entry.get('selected'):
            selected = entry['selected']
            image = ROOT / selected['path']
            check(image.is_file() and hashlib.sha256(image.read_bytes()).hexdigest()
                  == selected['sha256'], f'Selected source changed: {number}')
            check(not selected['unityAssets'] or (PACK / 'conversion.json').exists(),
                  f'Runtime outputs need conversion provenance: {number}')
    result = {'checks': checks, 'failures': failures, 'passed': not failures}
    (PACK / 'audit.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n')
    print(json.dumps(result, ensure_ascii=False))
    if failures:
        raise SystemExit(1)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    actions = parser.add_subparsers(dest='action', required=True)
    batch = actions.add_parser('prepare')
    batch.add_argument('ids', nargs='+', type=int, choices=range(1, 73))
    choice = actions.add_parser('select')
    choice.add_argument('id', type=int, choices=range(1, 73))
    choice.add_argument('candidate', choices=['a', 'b'])
    choice.add_argument('--note', required=True)
    actions.add_parser('report')
    actions.add_parser('audit')
    args = parser.parse_args()
    if args.action == 'prepare':
        prepare(args.ids)
    elif args.action == 'select':
        select(args.id, args.candidate, args.note)
    elif args.action == 'report':
        report()
    else:
        audit()


if __name__ == '__main__':
    main()
