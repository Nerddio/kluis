# Kluis

[![CI](https://github.com/Nerddio/kluis/actions/workflows/ci.yml/badge.svg)](https://github.com/Nerddio/kluis/actions/workflows/ci.yml)

Kluis (Dutch for "safe" or "vault") is a private, local-first personal finance app for Windows. You import your bank statements, Kluis categorises every transaction by keyword rules you control, and dashboards show your income, spending and savings over any period, compared against a salary history you enter yourself.

Inspired by local-first budgeting tools.

## Your data never leaves your machine

- Everything is stored in one encrypted database file, `%LOCALAPPDATA%\Kluis\kluis.db`, protected by a passphrase you choose (at least 12 characters). The folder is deliberately not Documents or the Desktop, which cloud backup tools such as OneDrive commonly sync off the machine.
- The file uses the SQLCipher 4 format through SQLite3 Multiple Ciphers: AES-256 encryption with a tamper check on every page, and a key derived from your passphrase with PBKDF2-HMAC-SHA512 at 256,000 iterations.
- "Remember on this PC" is off by default. If you tick it when creating or unlocking, the passphrase is stored in `%LOCALAPPDATA%\Kluis\kluis.remembered`, encrypted with Windows DPAPI so only your Windows account can read it, and Kluis then opens without asking. The trade-off: while it is on, anything running as your Windows user, including malware, can open your Kluis data. Turn it off on the home screen, which deletes the file.
- When a new version of Kluis needs to upgrade the database, it first copies it to `kluis-<UTC time>-before-migration.kluisbackup` in the same folder, encrypted with the same passphrase. If that copy fails, the upgrade does not run. Upgrades only move forward, so to go back to an older version of Kluis, restore the backup taken before the upgrade by renaming it to `kluis.db`.
- There is no server, no account, no cloud sync and no telemetry. The app makes no network calls at all.
- There is deliberately no passphrase recovery. If you forget it, the data cannot be opened, and encrypted backups use the same passphrase.
- No AI is involved: every categorisation and every flag is plain, testable code.

## Status

Early pre-release. The app installs, creates and unlocks its encrypted database, and can remember the passphrase on this PC. Statement import arrives in milestone M2.

## Build and test

Requires Windows 10 version 1809 or later, the .NET 10 SDK, and the WebView2 runtime (included with current Windows).

```
dotnet build
dotnet test
dotnet format --verify-no-changes
dotnet run --project src/Kluis.App -- --database "$env:TEMP\kluis-dev\kluis.db"
```

`--database <path>` points the app at a throwaway file. Without it the app uses your real database in `%LOCALAPPDATA%\Kluis`, so use it for development.

Schema changes are EF Core migrations. The `dotnet-ef` tool is a local tool pinned in `dotnet-tools.json`:

```
dotnet tool restore
dotnet ef migrations add <Name> --project src/Kluis.Data --output-dir Migrations
```

| Project | What it holds |
| --- | --- |
| `src/Kluis.App` | The WPF window that hosts the UI in a BlazorWebView |
| `src/Kluis.UI` | Razor components: every screen of the app |
| `src/Kluis.Core` | Domain logic with no UI dependency |
| `src/Kluis.Data` | EF Core, the encrypted SQLite connection and the migrations |
| `tests/Kluis.Core.Tests` | Unit tests (xUnit) |
| `tests/Kluis.IntegrationTests` | Integration tests against a real encrypted database file (xUnit) |
| `tests/Kluis.UI.Tests` | Component tests (bUnit) |
| `tests/Kluis.EndToEndTests` | End-to-end smoke tests (Playwright for .NET) that launch the real app window against a throwaway database |

`dotnet test` runs everything, including the end-to-end tests, which open and close the app window a few times. To skip them, add `--filter "Category!=E2E"`; to run only them, `--filter "Category=E2E"`. They attach to the WebView2 inside the app through a local debugging port, so no browser needs installing.

The same checks run in GitHub Actions on every pull request and on every push to `main` (`.github/workflows/ci.yml`): job `build-and-test` runs everything except the end-to-end tests.

**Known gap: the end-to-end tests do not run in CI.** On GitHub's hosted Windows runner the app window opens but the WebView2 inside it never starts, even with the WebView2 runtime present. Until that changes, the end-to-end smoke tests are run locally, with `dotnet test --filter "Category=E2E"`, before every release, and a release is not published if they fail.

`global.json` pins the SDK feature band so CI and local builds use the same compiler, and restores run in locked mode against the committed `packages.lock.json` files.

## Install

Download `KluisApp-win-Setup.exe` from the [Releases page](https://github.com/Nerddio/kluis/releases) and run it. Requires Windows 10 version 1809 or later with the WebView2 runtime, which current Windows includes.

What the installer does, plainly:

- Installs the app for your Windows user only, no administrator rights, into `%LOCALAPPDATA%\KluisApp`, and adds a Start menu shortcut.
- Your encrypted data is kept separately in `%LOCALAPPDATA%\Kluis`. Uninstalling removes `%LOCALAPPDATA%\KluisApp` and leaves your data in place; delete `%LOCALAPPDATA%\Kluis` yourself if you want it gone.
- The app makes no network calls: no update check, no telemetry. New versions are installed by downloading the next installer.
- The installer is not code-signed, so Windows SmartScreen shows "Windows protected your PC". Choose "More info", then "Run anyway". Signing costs money every year and is not in v1.

## Releasing

Every push to `main` builds and tests an installer in the Release workflow and keeps it as a build artefact for 30 days. Publishing a version is a deliberate step (Continuous Delivery):

1. Tag the commit on `main` with a semantic version and push the tag: `git tag v0.2.0` then `git push origin v0.2.0`. Versions below 1.0.0, or with a suffix such as `-beta.1`, are marked as pre-releases.
2. The Release workflow rebuilds nothing after its tests: it packs the installer once and attaches exactly those files to a **draft** release.
3. The end-to-end smoke test cannot run on GitHub's runner (see the known gap above), so run it on your own desktop against that installer: download `KluisApp-win-Setup.exe` from the draft, install it, then run
   `$env:KLUIS_APP_EXE = "$env:LOCALAPPDATA\KluisApp\current\Kluis.App.exe"; dotnet test tests/Kluis.EndToEndTests --filter "Category=E2E"`.
   The test uses a throwaway database, never your real one.
4. If it passes, publish the draft on GitHub. If it fails, delete the draft and the tag, fix, and tag again.

## Rolling back a bad release

- Mark the bad release as a pre-release on GitHub (or run `gh release edit v0.2.0 --prerelease`) so it is no longer shown as the latest. Fixing forward with a new patch release is the normal path.
- The previous installer stays downloadable on the Releases page. Uninstall, then install the older version.
- Database upgrades only move forward. If the bad version upgraded your database, restore the `kluis-<time>-before-migration.kluisbackup` file it wrote in `%LOCALAPPDATA%\Kluis` by renaming it to `kluis.db`, before opening the older version.

## Licence

MIT. See [LICENSE](LICENSE).
