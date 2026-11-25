# Kluis

Kluis (Dutch for "safe" or "vault") is a private, local-first personal finance app for Windows. You import your bank statements, Kluis categorises every transaction by keyword rules you control, and dashboards show your income, spending and savings over any period, compared against a salary history you enter yourself.

Inspired by local-first budgeting tools.

## Your data never leaves your machine

- Everything is stored in one encrypted database file in `%LOCALAPPDATA%\Kluis`, protected by a passphrase you choose.
- There is no server, no account, no cloud sync and no telemetry. The app makes no network calls at all.
- There is deliberately no passphrase recovery. If you forget it, the data cannot be opened, and encrypted backups use the same passphrase.
- No AI is involved: every categorisation and every flag is plain, testable code.

## Status

Under construction. The solution scaffold, the build pipeline and the first installable pre-release arrive in milestone M1 · Foundations. Until then there is nothing to build or install.

## Build and test

Requires Windows and the .NET SDK. Once the solution exists (M1):

```
dotnet build
dotnet test
```

## Install

From the end of M1, installers are published on the GitHub Releases page of this repository. The installers are not code-signed, so Windows SmartScreen will show a "Windows protected your PC" warning on first run.

## Licence

MIT. See [LICENSE](LICENSE).
