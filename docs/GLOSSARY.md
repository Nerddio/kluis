# Glossary

Terms used in this project, appended as they come up.

- **SDK (Software Development Kit)** — the compiler, libraries and command-line tools needed to build software for a platform; for .NET, installing the SDK gives you the `dotnet` command.
- **Remote repository** — the copy of a git repository hosted on a server such as GitHub, which local clones push to and pull from.
- **Definition of Done** — the agreed checklist that decides when something counts as finished.
- **Conventional commits** — a commit message format, `<type>: <summary>` (for example `feat: add import screen`), that makes the history readable and lets tools generate changelogs.
- **MIT licence** — a short permissive open-source licence: anyone may use, copy, modify and redistribute the code, provided the copyright notice is kept, with no warranty.
- **.gitignore** — a file listing paths git must never track, so local secrets, databases and build output cannot be committed by accident.
- **Secret scanning and push protection** — GitHub features that detect credentials in a repository; push protection rejects a push containing one before it becomes public.
- **Dependabot alerts** — GitHub's warnings when a dependency the project uses has a known security vulnerability.
- **CI (Continuous Integration)** — every change merges into the main branch often, and an automated pipeline builds it and runs the tests on every pull request.
- **Branch protection** — rules on a branch such as `main` that require a pull request and passing checks before merging, and block force pushes and deletion.
- **Ruleset** — GitHub's newer form of branch protection: a named set of rules (require a pull request, block force pushes, and so on) applied to matching branches, enforced only when its status is Active.
- **Static analysis** — examining source code for bugs or vulnerability patterns without running it; CodeQL is GitHub's static analysis tool.
- **Solution (.NET)** — a file (`Kluis.slnx`) that groups several projects so they build and test together; each project is one `.csproj` that compiles into one assembly.
- **Assembly** — the compiled output of one .NET project, a `.dll` or `.exe` file.
- **NuGet** — .NET's package manager and public package registry; a package reference in a `.csproj` pulls a library from it.
- **Target framework moniker (TFM)** — the string in a `.csproj` naming which .NET version and platform a project builds for, such as `net10.0` or `net10.0-windows10.0.17763.0`.
- **Razor component** — a `.razor` file mixing HTML markup and C#, the building block of a Blazor user interface, similar to a Vue single-file component.
- **BlazorWebView** — the WPF control that hosts Razor components inside an embedded WebView2 browser, with no web server and no network port.
- **Component test** — one user-interface component rendered and interacted with, without a real browser; in this project written with bUnit.
- **Unit test** — a test of one function or module in isolation, its dependencies replaced.
- **Integration test** — a test of two or more real parts together, usually with a real database.
- **.editorconfig** — a file declaring formatting rules (indentation, line endings) that editors and `dotnet format` follow.
- **Line endings (CRLF versus LF)** — Windows ends text lines with two characters (carriage return and line feed), Linux and macOS with one; `.gitattributes` controls the conversion git does on checkout.
- **Chrome DevTools Protocol (CDP)** — the remote-control interface of Chromium-based browsers, including WebView2; Playwright uses it to drive the app in end-to-end tests.
