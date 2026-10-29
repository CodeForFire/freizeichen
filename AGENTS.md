# Agent instructions

Freizeichen registers as the operating system's `tel:` handler and dials the number on a phone
that accepts AT commands over Bluetooth SPP. The Gigaset DL500A is the one it is tested with
(`docs/geraete.md` lists tested devices). It exists so that Lagebuch (CodeForFire/lagebuch) and every other program
in the ELW can keep emitting plain `tel:` links; see CodeForFire/lagebuch#617 for how the
protocol was found.

## The protocol

The DL500A offers a Serial Port Profile (SPP) on RFCOMM channel 1 and accepts AT commands on it:

```
AT                 → OK
AT+CGMI            → Gigaset Communications GmbH
ATD <number>       → OK, CLIP:EXTERN, CONNECT
```

The call stays up on the base after the link is closed, so a dial is: open the port, `AT`,
`ATD`, close. The OS exposes SPP as a serial port — an outgoing COM port on Windows,
`/dev/cu.*` on macOS, `/dev/rfcomm0` (after `rfcomm bind`) on Linux.

**The trap:** while the PC also holds the base's *audio* (Hands-Free) connection, every SPP
connection hangs until it times out instead of being refused. A timeout is therefore reported
with that hint, never as a generic error.

## Language

Issues, pull requests, commit messages, code and comments are **English**. The UI is **German**
and inline — no `.resx`. German terms of art stay German (Amtsholung, Ländervorwahl,
Einsatzkraft). Keep UI wording short and imperative; it is read in a command vehicle.

## Git

- Conventional Commits (`feat:`, `fix:`, `docs:`, `test:`, `build:`, `ci:`, `chore:`, …), English.
- DCO sign-off (`git commit -s`) and a cryptographic signature on every commit.
- SSH signing needs the key the command sandbox blocks: commit with the sandbox disabled and
  check `git cat-file commit HEAD | grep gpgsig` before pushing.
- Never push to `main`. Feature branch and pull request, always. Stack a pull request on another
  only when it needs that one's code; the body then says "Stacked on #N, review only the last
  commit".
- A pull request that touches a view carries before/after PNGs rendered with the headless Skia
  harness in `tests/Freizeichen.App.Tests`, attached with `gh pr create --attach`.
- The README's screenshots come from `ReadmeScreenshotTests`: run `make screenshots` and commit
  `docs/screenshots/` whenever a view they show changes. Fixtures use fictional numbers only.

## GitHub Actions

- Third-party actions are pinned to a full commit SHA of their latest release:
  `uses: owner/action@<sha> # vX.Y.Z`, resolved with
  `gh api repos/<owner>/<action>/releases/latest --jq .tag_name` and
  `gh api repos/<owner>/<action>/git/refs/tags/<tag> --jq .object.sha`.
- `.github/dependabot.yml` keeps the pins and the NuGet packages current.
- `zizmor` audits every workflow (`pipx run zizmor .` locally). Values from `inputs`, `needs` or
  `github.event` reach a `run:` through `env:`, never as `${{ }}` inside the script. Checkouts
  set `persist-credentials: false` unless the job pushes.

## Architecture

Dependencies point one way:

- `Domain` references nothing: `tel:` parsing, number normalisation, settings.
- `Modem` references `Domain`: the AT dial session over an `IAtChannel`, and the serial port.
- `AppLogic` references both and holds the view models. It has **no Avalonia reference**.
- `App` holds the views, `Program`, the composition root and the per-OS glue.

- View models are `sealed partial` classes over CommunityToolkit.Mvvm's `ObservableObject`, with
  `[ObservableProperty]` and `[RelayCommand]`.
- No DI container; wiring is by hand in the composition root. An optional dependency is a
  nullable constructor parameter with a default.
- Time is injected as a `TimeProvider`; tests use `FakeTimeProvider`. Never `DateTime.Now`.
- Non-view classes are `sealed`; domain values are `sealed record`s.
- Smallest change that fits; an interface only when there is a second implementation (a test
  double counts).

## C# and .NET

- Nullability is real: no `#nullable disable`, no `!` to silence a warning. Public entry points
  start with `ArgumentNullException.ThrowIfNull`.
- Failure has three shapes: `bool TryX(..., out T)` with `[MaybeNullWhen(false)]`, a documented
  nullable return, or a typed exception. No `Result<T>` library.
- Failures surface as a bound German status string on the view model; there is no logging
  framework. Every `catch (Exception)` carries a `[SuppressMessage("Design", "CA1031",
  Justification = "...")]` naming where the failure surfaces.
- Async: no `async void` outside event handlers, no `.Result`/`.Wait()`, no
  `ConfigureAwait(false)`. I/O APIs take `CancellationToken cancellationToken = default`.
- Culture: anything persisted uses `InvariantCulture`; identifiers compare `Ordinal`.
- `using`/`await using` for every disposable you own, never a manual `Dispose()` in `finally`.
- `Path.Join`, never `Path.Combine`.
- Package versions live in `Directory.Packages.props`; every project commits a
  `packages.lock.json` and CI restores with `--locked-mode`.

## Avalonia

Avalonia 12. Use the `avalonia-docs` MCP before reaching for an API from memory.

- Every `.axaml` declares `x:DataType`; compiled bindings stay on.
- App types are `internal` (CA1515): a view sets `x:ClassModifier="internal"` to match.
- Code-behind is view-only: focus and forwarding events to commands.
- Every text-less control gets an `AutomationProperties.Name`.
- Freizeichen opens from inside Lagebuch and wears Lagebuch's theme: `Theme/Tokens.axaml` and
  `Theme/Styles.axaml` are a trimmed copy of Lagebuch's, with the same token names so a change
  there carries across by diff. Use the tokens and classes (`display`, `overline`, `secondary`,
  `metric`, `status`, `Button.signal`), never a hard-coded colour or font in a view.
- Every new top-level `Window` calls `WindowsTitleBar.ApplyDarkMode` in `OnOpened`. Windows 10
  gets no dark title bar otherwise.

## Security

The number arrives from a `tel:` link, which any web page or document can produce: it is
**hostile input**. It reaches the phone only after `TelUri` has reduced it to digits and a
length cap — never a `;`, `\r`, `\n` or letter, which would let a link append its own AT
commands, and never `*` or `#`, which on a landline would let it set up call forwarding
(`*21*<number>#`) at the exchange. A security fix gets a test that replays the attack. Dialling always goes through the
confirm window; nothing dials silently.

## Tests

- xUnit (v3) with plain `Assert` and handwritten test doubles. No mocking library, no
  FluentAssertions.
- A test's name is a sentence in snake_case: `Rejects_a_semicolon_command`.
- Bug fixes: the failing test first. Headless UI tests use `[AvaloniaFact]`.
- No real `Task.Delay` or sleep in a test — drive time with `FakeTimeProvider`.

## Before you call it done

Run `make clean ci`, read the output (search for `Test Run Failed`), and review the diff for
dependency direction, hostile-input handling, German UI wording and automation names.

## Repository hygiene

Never commit `docs/superpowers/`.
