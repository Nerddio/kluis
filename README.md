# Kluis

[![CI](https://github.com/Nerddio/kluis/actions/workflows/ci.yml/badge.svg)](https://github.com/Nerddio/kluis/actions/workflows/ci.yml)

Kluis (Dutch for "safe" or "vault") is a private, local-first personal finance app for Windows. You import your bank statements, Kluis categorises every transaction by keyword rules you control, and dashboards show your income, spending and savings over any period, compared against a salary history you enter yourself.

Inspired by local-first budgeting tools.

## Your data never leaves your machine

- Everything is stored in one encrypted database file, `%LOCALAPPDATA%\Kluis\kluis.db`, protected by a passphrase you choose (at least 12 characters). The folder is deliberately not Documents or the Desktop, which cloud backup tools such as OneDrive commonly sync off the machine.
- The file uses the SQLCipher 4 format through SQLite3 Multiple Ciphers: AES-256 encryption with a tamper check on every page, and a key derived from your passphrase with PBKDF2-HMAC-SHA512 at 256,000 iterations.
- When a new version of Kluis needs to upgrade the database, it first copies it to `kluis-<UTC time>-before-migration.kluisbackup` in the same folder, encrypted with the same passphrase. If that copy fails, the upgrade does not run. Upgrades only move forward, so to go back to an older version of Kluis, restore the backup taken before the upgrade by renaming it to `kluis.db`.
- There is no server, no account, no cloud sync and no telemetry. The app makes no network calls at all.
- There is deliberately no passphrase recovery. If you forget it, the data cannot be opened, and encrypted backups use the same passphrase.
- No AI is involved: every categorisation and every flag is plain, testable code.

## Status

Under construction. The app creates and unlocks its encrypted database; "remember on this PC" and the first installable pre-release arrive later in milestone M1 · Foundations.

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

The same checks run in GitHub Actions on every pull request and on every push to `main` (`.github/workflows/ci.yml`, job `build-and-test`). `global.json` pins the SDK feature band so CI and local builds use the same compiler, and restores run in locked mode against the committed `packages.lock.json` files.

## Install

From the end of M1, installers are published on the GitHub Releases page of this repository. The installers are not code-signed, so Windows SmartScreen will show a "Windows protected your PC" warning on first run.

## Licence

MIT. See [LICENSE](LICENSE).
