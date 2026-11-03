"""Offline tests for release publication; no GitHub credentials or requests."""

import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location('publication', ROOT / 'scripts/publishing/publish.py')
publication = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(publication)


class GitHub:
    def __init__(self, existing=True, fail_upload=False, commits=None):
        self.existing = existing
        self.fail_upload = fail_upload
        self.calls = []
        self.commits = commits if commits is not None else [
            {'sha': f'{number:040x}', 'commit': {'message': f'fix: change {number}\n\nCommit body'}}
            for number in range(12, 0, -1)]

    def __call__(self, args, **kwargs):
        payload = json.loads(Path(args[args.index('--input') + 1]).read_text()) if '--input' in args else None
        self.calls.append((args, payload))
        if args[1:3] == ['release', 'upload']:
            if self.fail_upload:
                raise subprocess.CalledProcessError(1, args, stderr='upload failed')
            value = None
        else:
            endpoint = args[2].split('repos/test/player/')[1]
            if endpoint.startswith(('git/ref/tags/', 'releases/tags/')):
                if not self.existing:
                    raise subprocess.CalledProcessError(1, args, output='{"status":404}')
                value = {'id': 9, 'immutable': False}
            elif endpoint.startswith('commits?'):
                value = self.commits
            elif '/assets?' in endpoint:
                value = [{'id': 1, 'name': 'Luna_Player-v-0.6.0.zip'},
                         {'id': 2, 'name': 'Luna_Player-v-0.7.2.zip'},
                         {'id': 3, 'name': 'notes.txt'}]
            else:
                value = {'id': 9}
        return subprocess.CompletedProcess(args, 0, stdout=json.dumps(value))


class PublicationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.directory = Path(self.temporary.name)
        self.metadata = dict(version='0.7.2', sha='a' * 40, changes=['A "quoted" change.'],
                             archive='Luna_Player-v-0.7.2.zip', installer='Luna_Player-v-0.7.2.exe')
        (self.directory / 'release-metadata.json').write_text(json.dumps(self.metadata))
        assets = self.directory / 'release-assets'
        assets.mkdir()
        for name in [self.metadata['archive'], self.metadata['installer'], 'SHA256SUMS.txt']:
            (assets / name).write_text('fixture')

    def publish(self, channel='nightly', **options):
        remote = GitHub(**options)
        publication.publish(channel, self.directory, 'test/player', run=remote)
        return remote.calls

    def test_initial_nightly_creates_tag_once_and_publishes_prerelease(self):
        calls = self.publish(existing=False)
        created = [body for args, body in calls if args[2].endswith('/git/refs')]
        self.assertEqual([{'ref': 'refs/tags/nightly', 'sha': self.metadata['sha']}], created)
        final = calls[-1][1]
        self.assertTrue(final['prerelease'])
        self.assertFalse(final['draft'])
        self.assertEqual('false', final['make_latest'])

    def test_nightly_shows_ten_commit_subjects_from_the_built_commit(self):
        calls = self.publish()
        body = calls[-1][1]['body']
        self.assertTrue(body.startswith('### Latest Changes\n'))
        self.assertEqual(10, body.count('\n* ['))
        self.assertIn('fix: change 12', body)
        self.assertIn('fix: change 3', body)
        self.assertNotIn('fix: change 2', body)
        self.assertLess(body.index('fix: change 12'), body.index('fix: change 3'))
        self.assertNotIn('Commit body', body)
        self.assertNotIn(self.metadata['changes'][0], body)
        self.assertIn('https://github.com/test/player/commit/', body)
        self.assertTrue(any(args[2].endswith(f"commits?sha={self.metadata['sha']}&per_page=10")
                            for args, _ in calls))

    def test_short_history_lists_only_available_commits(self):
        calls = self.publish(commits=[{'sha': 'b' * 40, 'commit': {'message': 'Initial commit'}}])
        self.assertEqual(1, calls[-1][1]['body'].count('\n* ['))

    def test_stable_notes_still_use_metadata_changes(self):
        calls = self.publish('stable', existing=False)
        self.assertTrue(calls[-1][1]['body'].startswith('### Changes\n'))
        self.assertIn(self.metadata['changes'][0], calls[-1][1]['body'])
        self.assertFalse(any('/commits?' in args[2] for args, _ in calls))

    def test_history_failure_does_not_modify_the_nightly(self):
        remote = GitHub(existing=False)

        def fail_history(args, **options):
            if '/commits?' in args[2]:
                raise subprocess.CalledProcessError(1, args, stderr='history unavailable')
            return remote(args, **options)

        with self.assertRaises(subprocess.CalledProcessError):
            publication.publish('nightly', self.directory, 'test/player', run=fail_history)
        self.assertTrue(all(args[1] == 'api' and args[args.index('--method') + 1] == 'GET'
                            for args, _ in remote.calls))

    def test_existing_nightly_uploads_manifest_last_then_moves_tag(self):
        calls = self.publish()
        uploads = [args for args, _ in calls if args[1:3] == ['release', 'upload']]
        self.assertEqual(3, len(uploads))
        self.assertEqual('SHA256SUMS.txt', Path(uploads[-1][4]).name)
        self.assertTrue(all('--clobber' in args for args in uploads))
        self.assertEqual({'sha': self.metadata['sha'], 'force': True}, calls[-2][1])
        deletes = [args[2] for args, _ in calls if 'DELETE' in args]
        self.assertEqual(['repos/test/player/releases/assets/1'], deletes)

    def test_upload_failure_does_not_move_tag_or_finalize_release(self):
        remote = GitHub(fail_upload=True)
        with self.assertRaises(subprocess.CalledProcessError):
            publication.publish('nightly', self.directory, 'test/player', run=remote)
        self.assertFalse(any('PATCH' in args for args, _ in remote.calls))

    def test_existing_stable_tag_is_untouched(self):
        self.assertEqual(1, len(self.publish('stable')))

    def test_new_stable_release_uses_versioned_tag_without_force_updates(self):
        calls = self.publish('stable', existing=False)
        self.assertTrue(any(body and body.get('ref') == 'refs/tags/v0.7.2' for _, body in calls))
        self.assertFalse(calls[-1][1]['prerelease'])
        self.assertEqual('true', calls[-1][1]['make_latest'])
        self.assertFalse(any(body and body.get('force') for _, body in calls))

    def test_missing_assets_fail_before_any_remote_changes(self):
        (self.directory / 'release-assets/SHA256SUMS.txt').unlink()
        remote = GitHub()
        with self.assertRaises(FileNotFoundError):
            publication.publish('nightly', self.directory, 'test/player', run=remote)
        self.assertFalse(remote.calls)

    def test_version_change_removes_previous_nightly_filenames(self):
        self.metadata.update(version='0.8.0', archive='Luna_Player-v-0.8.0.zip', installer='Luna_Player-v-0.8.0.exe')
        (self.directory / 'release-metadata.json').write_text(json.dumps(self.metadata))
        for key in ['archive', 'installer']:
            (self.directory / 'release-assets' / self.metadata[key]).write_text('new version')
        calls = self.publish()
        self.assertEqual(['repos/test/player/releases/assets/1', 'repos/test/player/releases/assets/2'],
                         [args[2] for args, _ in calls if 'DELETE' in args])
        self.assertEqual('nightly', calls[-1][1]['tag_name'])

    def test_checksum_upload_failure_does_not_finalize_or_move_the_tag(self):
        remote = GitHub()

        def fail_checksum(args, **options):
            if args[1:3] == ['release', 'upload'] and Path(args[4]).name == 'SHA256SUMS.txt':
                raise subprocess.CalledProcessError(1, args, stderr='checksum upload failed')
            return remote(args, **options)

        with self.assertRaises(subprocess.CalledProcessError):
            publication.publish('nightly', self.directory, 'test/player', run=fail_checksum)
        self.assertFalse(any('PATCH' in args for args, _ in remote.calls))

    def test_lookup_permission_error_is_not_treated_as_a_missing_release(self):
        def forbidden(args, **options):
            raise subprocess.CalledProcessError(1, args, output='{"status":403}')

        with self.assertRaises(subprocess.CalledProcessError):
            publication.publish('nightly', self.directory, 'test/player', run=forbidden)

    def test_immutable_nightly_is_rejected_before_uploading(self):
        remote = GitHub()

        def immutable(args, **options):
            result = remote(args, **options)
            if args[1] == 'api' and '/releases/tags/' in args[2]:
                result.stdout = '{"id":9,"immutable":true}'
            return result

        with self.assertRaisesRegex(RuntimeError, 'immutability'):
            publication.publish('nightly', self.directory, 'test/player', run=immutable)
        self.assertFalse(any(args[1:3] == ['release', 'upload'] for args, _ in remote.calls))


if __name__ == '__main__':
    unittest.main()
