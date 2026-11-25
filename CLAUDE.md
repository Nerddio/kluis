# Kluis

Tier 1 — flagship.

## PROJECT

Kluis (Dutch for "safe" or "vault") is a Windows desktop app for personal finance that runs entirely on the user's own machine. The user imports bank statements; the app categorises every transaction by keyword rules the user edits, and shows dashboards of income, spending and savings over any period. It compares those figures against a salary history the user enters by hand — gross and net per month, raises over time with effective dates, a holiday allowance (vakantiegeld) as a configurable percentage of gross, and an optional year-end bonus defined as a configurable fraction of a chosen reference month's gross. It raises rule-based flags about spending. No AI or LLM (Large Language Model) is involved anywhere: every categorisation and every suggestion is plain, testable code.

Everything lives in one encrypted database file on the user's machine. There is no server, no account, no sync and no telemetry, and the app makes no network calls at all in v1.

Who it is for: primarily me, as my daily personal finance tool. Secondarily, anyone who wants to run it: the repository is public and installers are published on GitHub Releases, so another user installs it and gets their own, separate encrypted file. Nobody hosts anything.

Visibility: public repository, public releases, a portfolio entry with screenshots made from synthetic data only. The running app is never public — it exists only on users' own machines.

Inspiration: the idea of private, local-first budgeting from bank statement files was inspired by existing local-first budgeting tools. The README may say "inspired by local-first budgeting tools". Never copy text, branding, names, visual design or code from any existing product, and never describe this project as derived from or reverse-engineered from one.

Definition of Done for v1 (a Definition of Done is the agreed checklist that decides when something counts as finished):
- A user can install Kluis from a GitHub Release installer on Windows, create an encrypted database protected by a passphrase, and optionally choose "remember on this PC".
- The user can import CAMT.053 statement files; imports are idempotent (re-importing an overlapping period creates no duplicates) and each import is reconciled against the file's opening and closing balance.
- Transactions are categorised by editable keyword rules; the user can browse, search and filter transactions and recategorise them.
- An overview dashboard shows income, spending, savings and savings rate for any period (this month, last month, last 3 months, this year, last 12 months, all time, custom range), with a category breakdown and a month-by-month trend.
- The user can maintain a salary history (gross and net with effective dates, raises over time), configure holiday allowance and year-end bonus rules, and see expected gross income against the actual net salary payments detected in imported statements, and savings against net income.
- Rule-based flags work: spending spikes in a category against its recent average, newly detected recurring charges, and uncategorised transactions.
- The user can export transactions to CSV and create and restore an encrypted backup file.
- Every critical path is covered by tests that run in CI; the release installer is built and tested by the pipeline, never by hand.

## STACK

- C# on .NET, scaffolded with the official `dotnet new` templates, accepting whatever version they install. Justification: priority gap, transfers directly to the Microsoft-stack day job and to ASP.NET Core APIs later.
- UI: Blazor Hybrid hosted in a WPF window (BlazorWebView). Razor components (HTML and CSS plus C#) render inside WebView2 (the Edge browser engine embedded in a desktop window). Justification: reuses existing HTML/CSS skills with a component model close to Vue, and there is no local web server and therefore no open network port. Windows-only is acceptable and intended.
- Data: EF Core (Entity Framework Core — .NET's standard object-relational mapper, which maps C# classes to database tables and generates schema migrations) on SQLite (a complete database stored in one file, running inside the app's process).
- Encryption at rest: SQLite3 Multiple Ciphers via the `SQLite3MC.PCLRaw.bundle` NuGet package (MIT licence), used with `Microsoft.Data.Sqlite.Core` and the matching EF Core SQLite package that does NOT pull in the default unencrypted bundle. Never use `SQLitePCLRaw.bundle_e_sqlcipher` or `SQLitePCLRaw.bundle_e_sqlite3mc`: both are deprecated from SQLitePCLRaw 3.0. Configure the cipher scheme explicitly — never rely on the library's default — and prefer an authenticated scheme (one that also detects tampering). Record the choice in the encryption ADR.
- Key management: a passphrase is required at startup; the key is derived from it by a KDF (key derivation function — deliberately slow maths that turns a passphrase into a key and makes guessing expensive). Optional "remember on this PC" stores the secret protected by Windows DPAPI (Data Protection API — Windows encrypts it so only the same Windows user account can decrypt it), scope CurrentUser, in `%LOCALAPPDATA%`.
- Money: always `decimal`, never `double` or `float` — binary floating point cannot represent 0.10 exactly and drifts in sums.
- Statement import: CAMT.053 only in v1 — part of ISO 20022 (the international standard for financial messages), delivered as XML. Chosen because it is a structured standard rather than a bank-specific layout, so the parser is bank-agnostic, works for other users' banks, and carries opening and closing balances for reconciliation. Rejected for v1: MT940 (older SWIFT text format with a semi-structured, bank-specific description field), XLS/TXT exports (bank-specific layouts that change without notice), PDF (layout scraping, fragile).
- Charts: an established Blazor-compatible chart library, chosen in the dashboard milestone without deliberation.
- Unit and integration test runner: xUnit. Component tests: bUnit. End-to-end tests: Playwright for .NET (Microsoft.Playwright) driving the WebView2 control.
- Installer and release packaging: Velopack, published to GitHub Releases. Auto-update is NOT enabled in v1, because it would be the app's first network call.
- CI/CD: GitHub Actions on a GitHub-hosted Windows runner (free for public repositories).
- Hosting target: none. The app is distributed as an installer; there is nothing to host and no recurring cost.
- Rejected: plain WPF or Avalonia with XAML (native and mature, but XAML would be a second unfamiliar technology and UI end-to-end automation is clumsier); Tauri + React (lighter and closes the React gap, but the encrypted database goes through a Rust plugin layer and it does not touch .NET); Electron (heavy, larger attack surface); a browser PWA (Progressive Web App — browser storage can be wiped by clearing site data); PostgreSQL (a server database is the wrong tool for a single-user desktop app).

## TICKET AND LABEL STANDARD — project addition

no area labels at the start. If the backlog grows past roughly thirty open issues, propose at most four: `area:import`, `area:dashboard`, `area:salary`, `area:storage`.

## GITHUB ACCESS AND BOUNDARIES — project additions

Pull request checklist, project-specific tail (after the fixed core of seven items):

  - [ ] `dotnet format --verify-no-changes`, `dotnet build` and `dotnet test` pass locally
  - [ ] Fixtures, seed data and screenshots are synthetic only — no real statement, payslip, IBAN, name, bank, employer or amount, not even redacted; and no new network call was added

PROJECT ADDITION — MILESTONES (planned one at a time, per ROLE SPLIT AND PROCESS; only M1's issues are created up front):
  - M1 · Foundations — solution scaffold, encrypted database with passphrase unlock and "remember on this PC", the first EF Core migration, CI on every pull request, and a Velopack installer built by the pipeline and published as a first pre-release that opens an empty, unlocked app.
  - M2 · Import — CAMT.053 parser, idempotent import, balance reconciliation, import summary screen, synthetic CAMT.053 fixture generator.
  - M3 · Categorise and browse — keyword rules engine and editor, transaction list with search, filters and recategorisation.
  - M4 · Dashboards and salary — period selector, overview dashboard, category breakdown, monthly trend, salary history with raises, holiday allowance and year-end bonus rules, expected-versus-actual salary and savings-versus-net-income views.
  - M5 · Flags and v1.0.0 — rule-based flags, CSV export, encrypted backup and restore, README with synthetic-data screenshots, v1.0.0 release.

## STUDY MATERIAL — project addition

The question bank slug is `kluis`.

## TESTING — project specifics

  - Kinds that apply: unit, integration, component, end-to-end, smoke, regression. Contract tests do not apply — there is no second service.
  - Unit tests (xUnit): the CAMT.053 parser against golden fixtures (a golden file is a stored input whose expected output is checked in beside it), including credit and debit signs, multiple entries per statement, missing optional fields, and both structured and unstructured remittance information; the deduplication key; balance reconciliation; categorisation rule matching (priority order, case-insensitivity, no match); period boundaries and aggregation; savings and savings rate; the salary timeline (raises taking effect on their effective date); holiday allowance and year-end bonus calculation; every flag rule; CSV export escaping.
  - Integration tests (xUnit): EF Core against a REAL encrypted SQLite file in a temporary folder — no in-memory provider and no mocked database. The database here is an embedded file, not a server, so the real thing needs no container. Cover: migrations apply to a fresh encrypted file; opening with the wrong passphrase fails cleanly; the raw file on disk does not begin with the plain SQLite header (proving it is encrypted); re-importing the same file is idempotent; backup then restore round-trips every row; DPAPI "remember" unlocks only for the same Windows user.
  - Component tests (bUnit): the rules editor, the import summary, the period selector, the unlock screen's error states.
  - End-to-end tests (Playwright for .NET driving WebView2), the critical paths: first run → create passphrase → import a synthetic CAMT.053 file → overview shows the expected totals; restart → unlock with passphrase → data is present; add a categorisation rule → the transaction and the dashboard update. These double as the smoke pass on the packaged installer before a release is published. In M1, verify that the WebView2 end-to-end tests run on the GitHub-hosted Windows runner; if they cannot, run them locally before each release, say so in the README as a known gap, and record it in the containerisation and release ADR.
  - Synthetic data: a committed generator (a small C# tool in the repository) produces CAMT.053 files and seed data with invented counterparties, invented IBANs that cannot belong to a real account, and an invented bank identifier. Never commit a real export, not even redacted — a redacted file still reveals a bank's format, dates and amounts.
  - Deliberately not tested: chart rendering and the chart library itself, CSS and layout, EF Core and SQLite3 Multiple Ciphers internals, Velopack internals, WPF window chrome.

## DEPLOYMENT, CONTAINERS AND PIPELINES — project specifics

  - None of the three Docker uses applies, and that is the deliberate, recorded answer. Use 1: no service dependencies — SQLite runs inside the app's process, so there is nothing to bring up. Use 2: no service to start in the pipeline — integration tests run against a real encrypted SQLite file, which is already the real thing. Use 3: no server or backend; the deliverable is a Windows installer that runs on the user's machine. There is no Dockerfile and no .dockerignore. Record this in the containerisation and release ADR, and say plainly that this project does not close the Docker gap.
  - Hosting: none, cost €0. Releases are published to GitHub Releases on the public repository at no cost.
  - Pattern: pipeline owns everything. There is no host.
  - CD flavour: Continuous Delivery, not Continuous Deployment. Every merge to main produces a tested installer as a build artefact; publishing it as a release is a human decision made by pushing a version tag (`v1.2.3`). Reason: a release reaches other people's machines and their encrypted data, so it is a deliberate act, not a side effect of merging.
  - Jobs. `build-and-test` on every pull request and on main: restore, `dotnet format --verify-no-changes`, build, unit, component and integration tests. `e2e` on every pull request where the runner supports it (see TESTING). `package` on main and on tags: `dotnet publish` then Velopack packaging, uploaded as a build artefact. `release` on version tags only: runs the end-to-end smoke pass against the packaged build, then publishes exactly those files to a GitHub Release — no rebuild between the smoke pass and publishing. Only `release` gets `contents: write`; every other job is read-only.
  - Version numbers follow SemVer (Semantic Versioning — MAJOR.MINOR.PATCH, where a major bump signals a breaking change).
  - Database migrations: before applying any EF Core migration on startup, the app writes an automatic encrypted backup next to the database. A migration that destroys data is never shipped.
  - Rollback: a bad release is marked as a pre-release on GitHub so it is no longer presented as the latest, and the previous installer stays downloadable from the Releases page. Because migrations only move forward, the README tells users to restore the pre-migration backup when installing an older version. Fixing forward with a new patch release is the normal path.
  - Code signing: v1 installers are unsigned, so Windows SmartScreen shows a "Windows protected your PC" warning; the README explains this. Signing has a recurring cost and is a DEFAULT TO ACTION exception — propose it only if other users actually appear.
  - No auto-update in v1. Velopack's update check would be the app's first network call; it is a v2 decision, opt-in, and goes through SECURITY first.

## SECURITY — project threat model

  - The most likely harm is not an attacker — it is me leaking my own data into a public repository: a real statement used as a fixture, a screenshot with real figures, my bank's name in an issue, my salary or bonus rule in a commit message or a test, a debug log pasted into a ticket. Every rule in GITHUB ACCESS AND BOUNDARIES applies with extra force here. Never name my bank, my employer, my salary, my bonus arrangement or any account detail in code, tests, fixtures, comments, issues, commits, pull requests, ADRs, the glossary, the README or screenshots. My personal settings — including the specific year-end bonus fraction and reference month — exist only inside my local encrypted database. If something about my real data must be discussed during development, it goes in CLAUDE.local.md or in chat, never in the repository.
  - Someone with physical access to the laptop, or a stolen laptop: defended by encryption at rest with a passphrase-derived key.
  - Cloud backup sync silently copying the database off the machine: the database lives in `%LOCALAPPDATA%\Kluis`, never in Documents or the Desktop, which OneDrive commonly syncs. Explain this the first time it comes up.
  - A malicious or malformed statement file: the CAMT.053 parser treats every file as hostile. Teach XXE (XML External Entity attack — a crafted XML file that makes the parser read local files or make network requests) and disable DTD processing explicitly; enforce a file-size limit; reject rather than guess on malformed amounts or dates.
  - Logs and crash output: never log transaction descriptions, counterparties, IBANs, amounts, the passphrase or the key. Logs record event types and counts only.
  - Supply chain: NuGet dependencies kept minimal and watched by Dependabot; workflow permissions read-only by default; third-party GitHub Actions pinned to a full commit SHA, never a moving tag — explain why once.
  - Other users installing an unsigned installer: the README states plainly what the installer does, where the data lives, and that the app makes no network calls.
  - The "remember on this PC" option: an accepted, documented trade-off. It means anything running as the same Windows user — including malware — can open the database without the passphrase. The setting screen says so in one sentence; it is off by default.
  - Explicitly NOT defended in v1: malware already running as the user while the app is unlocked (it can read memory and screen; out of scope for a personal desktop app), and a forgotten passphrase (there is deliberately no recovery — the README and first-run screen say so; encrypted backups use the same passphrase).
  - GDPR (General Data Protection Regulation — the EU law on personal data) effectively does not apply: each user processes only their own household data on their own machine, and nothing is collected by the author.

## HOW WE WORK

Role split: I am the tech lead and product owner — I set requirements, decide scope and decide when something is done. Claude is the developer — it writes the code, runs git, commits, and maintains the issue tracker through `gh`. I push, I open every pull request, and I merge. No review is simulated: no review comments, no approvals, no second-engineer role-play; the "Least confident about" field is the substitute.

Phases (tier 1): SHAPE once at the start — the Definition of Done and scope in this document are the shape; write them into docs/SHAPE.md in the first session, then stop shaping. PLAN once per milestone — create that milestone's issues through the API in the standard format, show me the list afterwards. BUILD — one branch per ticket, conventional commits, tests in the same ticket, the push-and-pull-request block at the end of each ticket. CLOSE per milestone — append CHANGELOG.md, write any ADR that landed, add three to five questions to `kluis/INTERVIEW.md` in the study repository (at least one testing question and one pipeline or release question), create the next milestone's issues, propose any skill candidate.

The pipeline and a first installable pre-release are built in M1, not at the end.

The complete list of artefacts, so nothing creeps in later: docs/SHAPE.md; GitHub issues and milestones; up to six ADRs in docs/adr/; tests; the CI and release workflows; CHANGELOG.md appended at milestone close; docs/GLOSSARY.md; the README with run, build, release and rollback instructions; the private question bank in the study repository; CLAUDE.md. Nothing else — no C4 diagrams, ERD, OpenAPI contract, Definition of Ready, story points, staging environment, test strategy document, runbook, postmortem, retrospective document or status file unless I ask.

## REPOSITORY SETUP — project-specific parts

  - `.gitignore`: the standard `dotnet new gitignore` content, plus:
    `.env`
    `.env.local`
    `.env.*.local`
    `.env.production`
    `.claude/` (or `.claude/*` and `!.claude/skills/` on the next line if I answered yes to the skills question)
    `CLAUDE.local.md`
    `docs/INTERVIEW.md`
    `*.db`
    `*.db-shm`
    `*.db-wal`
    `*.sqlite`
    `*.sqlite3`
    `*.kluisbackup`
    `private/` (the only place a real statement may ever sit inside the working directory, and only temporarily for manual testing)
    `*.sta`
    `*.940`
    `Releases/`
    `publish/`
    `TestResults/`
    `playwright-report/`
    `test-results/`
    `*.pfx`
    `*.p12`
  - No `.env.example` — the app uses no environment variables. No `.dockerignore` — there is no Dockerfile.
  - Repository description: "Private, local-first personal finance for Windows. Import bank statements, see where your money goes. Encrypted, offline, no accounts." Topics: `dotnet`, `csharp`, `blazor-hybrid`, `wpf`, `sqlite`, `personal-finance`, `local-first`, `camt053`, `windows`.
  - Workflow files with `permissions: contents: read` at the top level and `contents: write` granted only on the `release` job — explain once that a token with write access is what a compromised dependency or action would abuse.

## ARTEFACTS

Likely ADRs (maximum six, each written after the decision is live, Context / Decision / Consequences, one page):
  - ADR-001 · Desktop stack: .NET Blazor Hybrid in WPF, over XAML-based WPF or Avalonia, Tauri + React, Electron and a browser PWA.
  - ADR-002 · Encryption at rest and key management: SQLite3 Multiple Ciphers with an explicitly chosen cipher scheme, passphrase-derived key, optional DPAPI "remember on this PC", no recovery.
  - ADR-003 · CAMT.053 as the only import format in v1, with idempotent import and balance reconciliation.
  - ADR-004 · Not containerised; distributed as a Velopack installer through GitHub Releases; Continuous Delivery by version tag; whether WebView2 end-to-end tests run in CI.
  - ADR-005 · Salary model: expected gross from user-entered history and configurable bonus rules, compared against actual net payments detected in statements; no payroll tax engine.
  - (Reserved for one decision that lands during the build.)

Definition of Done: as in PROJECT. A feature is not done until its critical path has a test; the app is not done until it installs from a Release built by the pipeline.

CI gates on: `build-and-test` (format check, build, unit, component and integration tests) always; `e2e` if it runs on the hosted runner; a failing test is never merged or skipped.

Question bank: three to five questions per milestone plus one per accepted ADR, batched at each milestone close into `kluis/INTERVIEW.md` in the study repository, weighted toward "defend" questions — for example "why is this not containerised", "why CAMT.053 and not the bank's spreadsheet export", "why decimal and not double", "why the integration tests use a real encrypted file and not an in-memory database".

## VOCABULARY FOCUS

  - encryption at rest — data encrypted where it is stored, so a copied file is unreadable without the key.
  - key derivation function (KDF) — turns a passphrase into an encryption key, deliberately slowly.
  - idempotency — doing an operation twice has the same effect as doing it once (re-importing a statement).
  - reconciliation — checking that recorded transactions agree with an independent total (opening balance + transactions = closing balance).
  - golden file / fixture — stored input data, with its expected output, that a test runs against (testing term).
  - build artefact and Continuous Delivery — the packaged output of a pipeline, and the practice of always having a releasable one while a human decides when to ship (pipeline term).
  - semantic versioning (SemVer).
  - code signing — cryptographically stamping an installer with the publisher's identity so Windows can verify where it came from.

## SCOPE

V1 (must ship):
  - Windows installer from GitHub Releases, built by the pipeline.
  - Encrypted database in `%LOCALAPPDATA%\Kluis`; passphrase unlock; optional "remember on this PC"; automatic encrypted backup before migrations.
  - CAMT.053 import: idempotent, reconciled against opening and closing balance, with an import summary.
  - One account in EUR (the data model has an accounts table from the first migration, because adding it later is a migration; the UI shows one account).
  - Keyword categorisation rules with an editor; default category set; recategorise a transaction.
  - Transaction list with search, filters (period, category, amount, direction) and sorting.
  - Overview dashboard: income, spending, savings, savings rate per period; category breakdown; monthly trend.
  - Salary history: gross and net with effective dates and raises; holiday allowance rule (percentage of gross, default 8%, configurable payment month); optional year-end bonus rule (fraction of a chosen month's gross, default off); expected gross versus detected actual net payments; savings versus net income.
  - Rule-based flags: category spending spikes against its recent average; newly detected recurring charges; uncategorised transactions.
  - CSV export; encrypted backup and restore.
  - English UI.

V2 (later):
  - MT940 and spreadsheet imports.
  - Payslip PDF import.
  - Budgets and targets per category.
  - Deeper dashboards: year-over-year, category drill-downs, longer-term savings trends.
  - Auto-lock after a period of inactivity.
  - Opt-in update check through Velopack (first network call — SECURITY review first).
  - UI translations (Portuguese, Dutch).
  - Multiple accounts in the UI.
  - Code signing, if other users appear.

Explicitly out of scope:
  - Bank API connections — PSD2 (the EU's second Payment Services Directive, which lets licensed third parties read bank accounts) aggregators or any live bank link.
  - Any server, cloud sync, accounts or telemetry.
  - LLMs or any AI.
  - Investment accounts and portfolio tracking (a separate, much later project).
  - PDF statement parsing.
  - Multi-currency.
  - Net-of-tax calculation of bonuses or any Dutch payroll tax engine.
  - macOS, Linux and mobile.
  - Financial advice: flags describe what happened in the user's own data; they never recommend products or investments.

## SKILL GAPS THIS CLOSES

  - C# / .NET — first real .NET application, from schema to UI to installer. Afterwards: ASP.NET Core APIs and .NET tooling at work become familiar territory, and .NET desktop or backend work is a credible thing to build.
  - Automated testing — all four main kinds (unit, integration against a real database, component, end-to-end) on a codebase where correctness about money matters. Afterwards: I can design and explain a testing strategy for any application, including why some things are deliberately not tested.
  - CI/CD — a pipeline that ships a real artefact to real users, with build-once-release-that, version tags and a rollback story. Afterwards: I can set up delivery for any project, not just deploy websites by hand.
  - Security (supporting, not on the named list) — encryption at rest, key management and hostile-input parsing.
  - Not closed, said plainly: Docker and PostgreSQL. The next tier 1 project should close them.

## WORKING AGREEMENTS

  - Claude decides alone: libraries (within STACK), file layout, naming, component structure, test design, workflow structure, chart library, cipher configuration details within ADR-002, label colours, refactors within a ticket. One line telling me what was decided.
  - Claude asks me: scope additions; anything expensive to reverse (schema changes needing a migration beyond the current milestone's plan, the encryption scheme once chosen, the release model); security trade-offs; anything recurring-cost such as code signing; anything touching my real data.
  - Claude creates and maintains issues, labels and milestones through `gh` in the standard format, with labels, milestone and assignee set in the same call.
  - Claude never pushes, never opens, edits, comments on, approves or merges a pull request, and never changes repository settings, branch protection or secrets.
  - Claude never bulk-stages, runs `git status` before every commit, and never commits secrets, local tooling, real data or `.claude/`.
  - Claude takes no credit anywhere — no co-author trailer, no "Generated with" line.
  - Conventional commits; one branch, one ticket, one pull request.
  - Tests ship inside the ticket that adds the behaviour.
  - The pipeline and a first installable pre-release are built in M1.
  - Never write my bank, employer, salary, bonus arrangement or any account detail anywhere in the repository or on GitHub.
  - Progress readout (📍 M<N> of 5 — ▓░ <closed>/<total> tickets) on a merge, a milestone close or when I ask, from real GitHub numbers.
  - Status emoji only in chat; never in commits, issues, pull requests, ADRs, the README or code.
  - Every session ends with a named next ticket. If roughly a month passes with no merged pull request, say so and ask me to resume, pause or archive.

## CONSTRAINTS

  - Budget: €0 recurring. No hosting, no paid services. Code signing would be the only possible cost and is not in v1.
  - Privacy: real statements and payslips never enter the repository, its history, GitHub issues or screenshots; real files used for manual testing sit only in the gitignored `private/` folder or outside the repository, and are removed afterwards.
  - Legal: MIT licence. No text, branding, visual design or code copied from any existing product. The name "Kluis" is a common Dutch word; before the first public release, I check the EUIPO trademark register myself.
  - Platform: Windows only.
  - No hours, no deadlines.
