"""Publish validated build assets through gh; only the nightly tag may move."""

import argparse
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile
import sys
from datetime import datetime, timezone


def publish(channel, directory, repository, run=subprocess.run):
    directory = Path(directory)
    metadata = json.loads((directory / 'release-metadata.json').read_text(encoding='utf-8-sig'))
    tag = 'nightly' if channel == 'nightly' else 'v' + metadata['version']
    names = [metadata['archive'], metadata['installer'], 'SHA256SUMS.txt']
    assets = [directory / 'release-assets' / name for name in names]
    for asset in assets:
        if not asset.is_file():
            raise FileNotFoundError(asset)

    def gh(*args):
        return run(['gh', *args], check=True, capture_output=True, text=True).stdout

    def api(endpoint, method='GET', data=None, missing_ok=False):
        args = ['api', f'repos/{repository}/{endpoint}', '--method', method]
        # Structured JSON preserves booleans and arbitrary release-note text.
        with tempfile.TemporaryDirectory(prefix='luna-release-') as temporary:
            if data is not None:
                payload = Path(temporary) / 'payload.json'
                payload.write_text(json.dumps(data), encoding='utf-8')
                args += ['--input', str(payload)]
            try:
                output = gh(*args)
            except subprocess.CalledProcessError as error:
                try:
                    status = json.loads(error.stdout or '{}').get('status')
                except json.JSONDecodeError:
                    status = None
                if missing_ok and str(status) == '404':
                    return None
                raise
        return json.loads(output) if output.strip() else None

    ref = api(f'git/ref/tags/{tag}', missing_ok=True)
    if channel == 'stable' and ref is not None:
        return
    release = api(f'releases/tags/{tag}', missing_ok=True)
    if release and release.get('immutable'):
        raise RuntimeError('The nightly release must allow tag and asset updates; release immutability is enabled.')
    if channel == 'nightly':
        commits = api(f"commits?sha={metadata['sha']}&per_page=10")
        notes = '\n'.join(
            f"* [{commit['sha'][:7]}](https://github.com/{repository}/commit/{commit['sha']}) "
            + commit['commit']['message'].splitlines()[0]
            for commit in commits[:10])
        body = f"### Latest Changes\n{notes}\n\nBuild from commit {metadata['sha']}."
        built = metadata.get('built_at') or datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M UTC')
        body += f"\n\nNightly build: {built}."
        # Publish the identity embedded in the binaries only after all packages and checksums.
        if not metadata.get('build_id'):
            raise ValueError('Nightly metadata must contain a build_id.')
        metadata['changes'] = body
        manifest = directory / 'release-metadata.json'
        manifest.write_text(json.dumps(metadata), encoding='utf-8')
        assets.append(manifest)
        names.append(manifest.name)
        run_id = os.environ.get('GITHUB_RUN_ID')
        if run_id:
            body += f"\n[Build run](https://github.com/{repository}/actions/runs/{run_id})."
    else:
        notes = '\n'.join('* ' + change for change in metadata['changes'])
        body = f"### Changes\n{notes}\n\nBuild from commit {metadata['sha']}."
    if ref is None:
        api('git/refs', 'POST', {'ref': 'refs/tags/' + tag, 'sha': metadata['sha']})
    fields = {
        'tag_name': tag,
        'name': 'Nightly Build' if channel == 'nightly' else 'Luna Player v' + metadata['version'],
        'body': body, 'prerelease': channel == 'nightly',
        'make_latest': 'false' if channel == 'nightly' else 'true',
    }
    if release is None:
        release = api('releases', 'POST', {**fields, 'draft': True})

    # Upload checksums last. A failed binary upload must not advertise a new manifest.
    for asset in assets:
        args = ['release', 'upload', tag, str(asset), '--repo', repository]
        if channel == 'nightly':
            args.append('--clobber')
        gh(*args)

    if channel == 'nightly':
        current = api(f"releases/{release['id']}/assets?per_page=100")
        for asset in current:
            if asset['name'] not in names and re.fullmatch(r'Luna_Player-v-.+\.(zip|exe)', asset['name']):
                api(f"releases/assets/{asset['id']}", 'DELETE')
        # Changing target_commitish on a release does not move an existing tag.
        api('git/refs/tags/nightly', 'PATCH', {'sha': metadata['sha'], 'force': True})
    api(f"releases/{release['id']}", 'PATCH', {**fields, 'draft': False})


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--channel', choices=['stable', 'nightly'], required=True)
    parser.add_argument('--directory', type=Path, required=True)
    arguments = parser.parse_args()
    try:
        publish(arguments.channel, arguments.directory, os.environ['GITHUB_REPOSITORY'])
    except subprocess.CalledProcessError as error:
        print(error.stderr or str(error), file=sys.stderr)
        raise SystemExit(error.returncode) from None
