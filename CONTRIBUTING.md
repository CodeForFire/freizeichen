# Contributing to Freizeichen

Thanks for your interest in contributing! Freizeichen dials `tel:` links on a Gigaset DL500A over
Bluetooth, so that [Lagebuch](https://github.com/CodeForFire/lagebuch) and every other program in
the ELW can offer one-click calling. Every contribution is welcome, from a typo fix to support for
another phone.

## Before you start

- **Found a bug or have an idea?** Open an issue with the templates, or start a
  [Discussion](https://github.com/CodeForFire/freizeichen/discussions) if you are unsure.
- **Another phone?** If your base station also speaks AT commands over Bluetooth SPP, open an
  issue with what `AT`, `AT+CGMI` and `ATD` return on it. That transcript is worth more than a
  patch.
- **Bigger changes:** say what you plan in an issue first. Asking early costs everyone less than a
  pull request that goes the wrong way.

## Language

Issues, pull requests, commit messages, code and comments are in **English**, so that someone who
does not speak German can read the backlog and work here.

The **UI is German**, and its text is inline (there are no `.resx` files). German terms of art stay
German even in English text: Amtsholung, Ländervorwahl, Einsatzkraft, Lagebuchführer. Quoted UI
strings are reproduced verbatim, never translated. Keep UI wording short and imperative, because
it is read in a command vehicle.

**Discussions are exempt:** German and English are both fine there.

## Development setup

- [.NET SDK](https://dotnet.microsoft.com/download/dotnet). The version is pinned in
  [`global.json`](global.json) (10.x).
- `make`, plus Python 3 for the fake phone (Linux only).

```bash
make ci                          # restore (locked), build, test, format check: what CI runs
make run                         # settings window
make run ARGS=tel:0891234567     # dial window
make help                        # everything else
```

You do not need a DL500A. On Linux, `make fake-phone` plays one on `/dev/rfcomm9` and prints every
line Freizeichen sends; `SCENARIO=ok|error|busy|nocarrier|nodialtone|silent` picks how it answers
`ATD`. See the [README](README.md#ohne-telefon-testen).

Before you push, run a clean build, `make clean ci`, and read the output. The last line can say
`0 Error(s)` even when a test assembly failed, so search the output for `Test Run Failed`.

## Code conventions

[`AGENTS.md`](AGENTS.md) has them in full: the dependency direction between the four projects,
view models without an Avalonia reference, injected `TimeProvider`, nullability, failure handling
and the Avalonia rules. The analyzers run at `AnalysisMode=All` with warnings as errors, so most
style questions are answered by the build.

Tests use xUnit with plain `Assert` and handwritten test doubles, with no mocking library. Name
each test as a sentence in snake_case, e.g. `Rejects_a_semicolon_command`. A bug fix starts with
the failing test.

## Commit conventions

Every commit must meet three requirements:

1. a [Conventional Commits](https://www.conventionalcommits.org/) subject line,
2. a DCO `Signed-off-by` trailer, added by `git commit -s`,
3. a cryptographic signature, SSH or GPG, added by `git commit -S`.

```bash
git commit -s -S -m "fix(modem): report a timeout with the audio-connection hint"
```

The subject line starts with a type prefix (`feat:`, `fix:`, `docs:`, `style:`, `refactor:`,
`perf:`, `test:`, `build:`, `ci:` or `chore:`), optionally scoped, e.g. `fix(modem):`.

You need both the sign-off and the signature, and they mean different things. The sign-off is
about **rights**: it certifies you may submit the change under the project's licence (see
[Licence](#licence)). The signature is about **identity**: it proves the commit came from you.

### Signing your commits

Set this up once, and from then on `git commit -s` is all you type.

**SSH** is recommended, because you already have a key:

```bash
git config --global gpg.format ssh
git config --global user.signingkey ~/.ssh/id_ed25519.pub
git config --global commit.gpgsign true
```

Then upload the same public key to GitHub **a second time**, as a signing key: *Settings → SSH and
GPG keys → New SSH key →* **Key type: Signing Key**. This is the step people miss. Without it your
commits are signed but show as *Unverified*.

**GPG** works too; follow GitHub's
[Telling Git about your signing key](https://docs.github.com/en/authentication/managing-commit-signature-verification/telling-git-about-your-signing-key).

### Fixing a branch you already pushed

If a commit slipped through unsigned or without a sign-off, rewrite the branch rather than adding
a commit on top:

```bash
git rebase --signoff --gpg-sign origin/main
git push --force-with-lease
```

Force-pushing your own pull-request branch is expected here.

## Pull requests

- Branch from `main`, and keep one change per pull request.
- `make clean ci` is green.
- **UI changes** include a before/after pair of screenshots, rendered with the headless harness in
  `tests/Freizeichen.App.Tests` (`Window.CaptureRenderedFrame()`). If a view the README shows has
  changed, run `make screenshots` and commit `docs/screenshots/`.
- **The dial path** still goes through the confirm window, and the number still reaches the phone
  only through `TelUri`. A change there needs a test that replays the attack it guards against.
- **GitHub Actions** are pinned to a full commit SHA, and `zizmor .` stays clean.
- Screenshots, tests and examples use **fictional phone numbers** only.

The PR template has this checklist.

## Reporting issues

Use the issue templates. For a bug, include your OS, the Freizeichen version, the phone model and
the port you selected. Security vulnerabilities do **not** belong in public issues; see
[`SECURITY.md`](SECURITY.md).

## AI-assisted contributions

Contributions made with coding agents are welcome if they follow the conventions above.
[`AGENTS.md`](AGENTS.md) documents them in the form agents read.

## Licence

By contributing, you agree that your contributions will be licensed under the
[MIT licence](LICENSE). Signing off your commits (`git commit -s`) certifies that you wrote the
change or otherwise have the right to submit it under the project's licence
([Developer Certificate of Origin](https://developercertificate.org/)).
