# Kluis — Shape

## Problem statement

Kluis (Dutch for "safe" or "vault") is a Windows desktop app for personal finance that runs entirely on the user's own machine. The user imports bank statements; the app categorises every transaction by keyword rules the user edits, and shows dashboards of income, spending and savings over any period. It compares those figures against a salary history the user enters by hand — gross and net per month, raises over time with effective dates, a holiday allowance (vakantiegeld) as a configurable percentage of gross, and an optional year-end bonus defined as a configurable fraction of a chosen reference month's gross. It raises rule-based flags about spending. No AI or LLM (Large Language Model) is involved anywhere: every categorisation and every suggestion is plain, testable code.

Everything lives in one encrypted database file on the user's machine. There is no server, no account, no sync and no telemetry, and the app makes no network calls at all in v1.

## Who uses it

Primarily me, as my daily personal finance tool. Secondarily, anyone who wants to run it: the repository is public and installers are published on GitHub Releases, so another user installs it and gets their own, separate encrypted file. Nobody hosts anything.

Visibility: public repository, public releases, a portfolio entry with screenshots made from synthetic data only. The running app is never public — it exists only on users' own machines.

## Definition of Done for v1

A Definition of Done is the agreed checklist that decides when something counts as finished.
- A user can install Kluis from a GitHub Release installer on Windows, create an encrypted database protected by a passphrase, and optionally choose "remember on this PC".
- The user can import CAMT.053 statement files; imports are idempotent (re-importing an overlapping period creates no duplicates) and each import is reconciled against the file's opening and closing balance.
- Transactions are categorised by editable keyword rules; the user can browse, search and filter transactions and recategorise them.
- An overview dashboard shows income, spending, savings and savings rate for any period (this month, last month, last 3 months, this year, last 12 months, all time, custom range), with a category breakdown and a month-by-month trend.
- The user can maintain a salary history (gross and net with effective dates, raises over time), configure holiday allowance and year-end bonus rules, and see expected gross income against the actual net salary payments detected in imported statements, and savings against net income.
- Rule-based flags work: spending spikes in a category against its recent average, newly detected recurring charges, and uncategorised transactions.
- The user can export transactions to CSV and create and restore an encrypted backup file.
- Every critical path is covered by tests that run in CI; the release installer is built and tested by the pipeline, never by hand.

## Explicitly out of scope

  - Bank API connections — PSD2 (the EU's second Payment Services Directive, which lets licensed third parties read bank accounts) aggregators or any live bank link.
  - Any server, cloud sync, accounts or telemetry.
  - LLMs or any AI.
  - Investment accounts and portfolio tracking (a separate, much later project).
  - PDF statement parsing.
  - Multi-currency.
  - Net-of-tax calculation of bonuses or any Dutch payroll tax engine.
  - macOS, Linux and mobile.
  - Financial advice: flags describe what happened in the user's own data; they never recommend products or investments.

## Stack and hosting

C# on .NET with Blazor Hybrid in a WPF window, EF Core on SQLite encrypted with SQLite3 Multiple Ciphers, CAMT.053 import, xUnit, bUnit and Playwright for .NET, packaged with Velopack and published to GitHub Releases by GitHub Actions. No hosting: the app is an installer that runs on the user's own machine, at a recurring cost of €0. The full stack and the reasons for it are in CLAUDE.md.
