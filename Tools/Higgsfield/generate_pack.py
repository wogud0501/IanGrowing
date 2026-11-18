#!/usr/bin/env python3
"""Bounded Higgsfield jobs. Explicit asset IDs only; completed requests never resubmit."""
import argparse
import concurrent.futures
import json
import subprocess
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'ArtSource/Higgsfield'


def generate(asset_id, model, params):
    prompt_path = SOURCE / 'Prompts' / (asset_id + '.txt')
    if not prompt_path.exists():
        raise ValueError('Missing prompt: ' + asset_id)
    result_path = SOURCE / 'Raw' / (asset_id + '.result.json')
    error_path = SOURCE / 'Raw' / (asset_id + '.stderr.log')
    request_path = SOURCE / 'Raw' / (asset_id + '.request.json')
    if request_path.exists() and not result_path.exists():
        return asset_id, 'Previously submitted; inspect before retry'
    if not result_path.exists():
        cmd = ['higgsfield', 'generate', 'create', model, '--prompt', prompt_path.read_text()]
        for name, value in [('quality', 'high'), ('aspect_ratio', '16:9'), ('resolution', '2k')]:
            definition = params.get(name)
            if definition and (not definition.get('enum') or value in definition['enum']):
                cmd += ['--' + name, value]
        cmd += ['--wait', '--wait-timeout', '15m', '--json']
        refs = SOURCE / 'Prompts' / (asset_id + '.refs.json')
        if refs.exists():
            for reference in json.loads(refs.read_text()):
                cmd += ['--image', str(ROOT / reference)]
        request_path.write_text(json.dumps({'assetId': asset_id, 'model': model,
                                            'prompt': str(prompt_path.relative_to(ROOT)),
                                            'references': json.loads(refs.read_text()) if refs.exists() else []}, indent=2))
        with result_path.open('w') as output, error_path.open('w') as error:
            finished = subprocess.run(cmd, stdout=output, stderr=error, cwd=ROOT)
        if finished.returncode:
            return asset_id, 'FAILED; inspect stderr; no automatic retry'
    try:
        jobs = json.loads(result_path.read_text())
    except (ValueError, FileNotFoundError):
        return asset_id, 'Pending or invalid result; inspect before retry'
    if isinstance(jobs, dict):
        jobs = [jobs]
    for i, job in enumerate(jobs):
        url = job.get('result_url')
        if not url or job.get('status') != 'completed':
            continue
        image = SOURCE / 'Raw' / (asset_id + ('_' + str(i + 1) if len(jobs) > 1 else '') + '.png')
        if not image.exists():
            urllib.request.urlretrieve(url, image)
    return asset_id, 'Downloaded; QA required'


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('assets', nargs='+')
    parser.add_argument('--workers', type=int, choices=[1, 2, 3, 4], default=2)
    parser.add_argument('--model', help='Optional discovered job_type; otherwise choose an available image model')
    args = parser.parse_args()
    if len(args.assets) > 32:
        parser.error('Maximum 32 explicitly selected candidates per invocation')
    models = json.loads(subprocess.check_output(['higgsfield', 'model', 'list', '--json'], text=True))
    available = {entry['job_type'] for entry in models if entry.get('type') == 'image'}
    choices = [args.model] if args.model else ['gpt_image_2_6', 'gpt_image_2_5', 'gpt_image_2', 'nano_banana_flash']
    model = next((choice for choice in choices if choice in available), None)
    if not model:
        parser.error('No requested compatible model is available; inspect the live catalog. No jobs submitted.')
    schema = json.loads(subprocess.check_output(['higgsfield', 'model', 'get', model, '--json'], text=True))
    params = {definition['name']: definition for definition in schema.get('params', [])}
    with concurrent.futures.ThreadPoolExecutor(max_workers=args.workers) as pool:
        for asset_id, status in pool.map(lambda asset: generate(asset, model, params), args.assets):
            print(asset_id + ': ' + status, flush=True)


if __name__ == '__main__':
    main()
