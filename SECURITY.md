# Security Policy

## Supported versions

Freizeichen is pre-1.0. Only the **latest release** receives security fixes.

| Version | Supported |
|---------|-----------|
| latest release | :white_check_mark: |
| earlier releases | :x: |

## Reporting a vulnerability

**Please do not report security vulnerabilities through public GitHub issues, discussions or pull
requests.**

Use [GitHub's private vulnerability reporting](https://github.com/CodeForFire/freizeichen/security/advisories/new)
instead:

1. Go to the repository's **Security** tab.
2. Click **Report a vulnerability**.
3. Describe the issue, including:
   - the affected version (installer file name or tag, e.g. `v0.1.0`);
   - the platform: Windows, macOS or Linux;
   - steps to reproduce, or a proof of concept (a crafted `tel:` link is ideal);
   - the impact you think it has.

Reports are reviewed promptly. Please give us reasonable time to fix the issue before you disclose
it publicly.

## What matters here

Freizeichen is the operating system's `tel:` handler, so **any web page, e-mail or document can
hand it input**. Whatever reaches the phone after `ATD` is executed by the phone. Within scope:

- **Getting anything other than a phone number to the phone.** `TelUri` reduces a link to digits
  and an optional leading `+`, capped at 32 digits and 256 characters. A way to get a `;`, `\r`,
  `\n` or letter through would let a link append its own AT commands. A `*` or `#` would let it
  dial service codes, such as `*21*<number>#`, which sets up call forwarding at the exchange.
- **Dialling without confirmation.** Every link must go through the confirm window, and a keypress
  in the first second after the window opens must not dial. Any path that places a call without
  the Lagebuchführer pressing *Wählen* is a vulnerability.
- **The `tel:` registration**, meaning the Windows registry entries and the macOS bundle claim. For
  example, a way for Freizeichen's registration to be hijacked, or to hijack another scheme.
- **Settings file handling**, such as path or deserialization issues in the stored settings.
- **The release supply chain**: the build workflows, pinned actions, and the published installers
  and `SHA256SUMS.txt`.

## Out of scope

- The Bluetooth security of the DL500A itself (pairing, SPP access by another paired device).
  Report those to Gigaset.
- Anyone who can already run code as the logged-in user can open the serial port directly and
  needs no help from Freizeichen.
- The installers are not code-signed yet. This is a known limitation, not a vulnerability.
  Verify downloads with `SHA256SUMS.txt`.

## Non-security issues

Please use the [issue templates](https://github.com/CodeForFire/freizeichen/issues/new/choose).
