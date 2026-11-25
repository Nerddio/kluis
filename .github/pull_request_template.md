## Ticket

Closes #

<!-- If there is no ticket, say why. Documentation corrections and scaffolding may legitimately have none. -->

## What changed

<!-- What a reviewer needs to know, not a restatement of the diff. -->

## How it was verified

<!-- What you actually ran or checked. Not what you intend to check. -->

## Review checklist

- [ ] Acceptance criteria demonstrably met
- [ ] All local checks pass
- [ ] Tests added or updated for the behaviour this changes
- [ ] Documentation updated, if behaviour or setup changed
- [ ] New terms defined in `docs/GLOSSARY.md`
- [ ] Any new dependency justified above
- [ ] No secrets, `.env` files or local tooling in the diff
- [ ] `dotnet format --verify-no-changes`, `dotnet build` and `dotnet test` pass locally
- [ ] Fixtures, seed data and screenshots are synthetic only — no real statement, payslip, IBAN, name, bank, employer or amount, not even redacted; and no new network call was added

## Least confident about

<!-- Required. What in this change is most likely to be wrong, and why? If genuinely nothing, say why not. "Nothing" on its own is not an answer. -->
